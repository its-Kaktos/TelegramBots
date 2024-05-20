using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;
using Serilog;
using Serilog.Context;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using TelegramBots.MessageSender.Data;
using TelegramBots.MessageSender.DTOs;
using TelegramBots.MessageSender.Services.BotClients;
using TelegramBots.MessageSender.Services.Queues;
using TelegramBots.MessageSender.TelegramBotClientExtensions;
using File = System.IO.File;

namespace TelegramBots.MessageSender.Services.MessageSenders;

public abstract class BaseMessagesHostedService<TBotClient, TOptions> :
    BackgroundService where TOptions : TelegramBotClientOptions where TBotClient : BaseTelegramBotClient<TOptions>
{
    private readonly ILogger _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly BaseOperationsQueueService _operationsQueueService;
    private readonly FileCacheService _fileCacheService;
    private readonly IServiceProvider _serviceProvider;

    protected BaseMessagesHostedService(ILogger logger, IMemoryCache memoryCache, BaseOperationsQueueService operationsQueueService, FileCacheService fileCacheService, IServiceProvider serviceProvider)
    {
        _memoryCache = memoryCache;
        _operationsQueueService = operationsQueueService;
        _fileCacheService = fileCacheService;
        _serviceProvider = serviceProvider;
        _logger = logger.ForContext<BaseMessagesHostedService<TBotClient, TOptions>>();
    }

    internal async Task HandleMessageQueueForChatAsync(ObjectPool<TBotClient> telegramBotClientPool,
        Type cacheDbContextType,
        long chatId,
        ProcessMessageDto processMessageDto,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("ChatId", chatId))
        {
            _logger
                .ForContext("MessagesGroupCount", processMessageDto.MessagesGroup.Count)
                .Debug("Started new task for answering bot messages");

            var botClient = telegramBotClientPool.Get();
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var cacheDbContext = (ICacheDbContext)scope.ServiceProvider.GetRequiredService(cacheDbContextType);
                while (!processMessageDto.MessagesGroup.IsEmpty)
                {
                    foreach (var (guidId, messagesQueue) in processMessageDto.MessagesGroup)
                    {
                        await HandleMessageQueueAsync(botClient, cacheDbContext, messagesQueue, chatId, guidId, processMessageDto, cancellationToken);
                    }
                }
            }
            catch (Exception e)
            {
                _logger
                    .ForContext("MessagesGroupCount", processMessageDto.MessagesGroup.Count, true)
                    .ForContext("MessagesGroup", processMessageDto.MessagesGroup.AsEnumerable(), true)
                    .Error(e, "Exception occured while sending message to user");
            }
            finally
            {
                telegramBotClientPool.Return(botClient);
            }
        }
    }

    private async Task HandleMessageQueueAsync(ITelegramBotClient botClient, ICacheDbContext cacheDbContext,
        ConcurrentQueue<MessageDto> messagesQueue,
        long chatId, string guidId,
        ProcessMessageDto processMessageDto,
        CancellationToken cancellationToken = default)
    {
        var operationToDo = GetOperationToDo(messagesQueue);

        if (messagesQueue.IsEmpty)
        {
            processMessageDto.MessagesGroup.TryRemove(guidId, out var removedMessagesQueue);
            AddRemainingMessagesInQueueToBeProcessedAgain(removedMessagesQueue);
        }

        if (operationToDo is null) return;

        var retryAgain = false;
        do
        {
            try
            {
                if (TryGetRetryAfter(chatId, out var retryAfter))
                {
                    if (retryAfter > DateTimeOffset.Now)
                    {
                        var retryAfterMilliseconds = (retryAfter.Value - DateTimeOffset.Now).TotalMilliseconds;
                        await Task.Delay((int)retryAfterMilliseconds, cancellationToken);
                    }

                    RemoveRetryAfterIfExists(chatId);
                }

                // For some reason files are not removed after uploading them to telegram.
                // to fix this run a cronjob to remove files with creation time of older than
                // 24H
                await HandleMessageAsync(botClient, cacheDbContext, operationToDo);
                await Task.Delay(100, cancellationToken);
            }
            catch (ApiRequestException e) when (e.Parameters?.RetryAfter is not null)
            {
                retryAgain = true;
                var retryAfter = e.Parameters.RetryAfter.Value;
                SetRetryAfter(chatId, DateTimeOffset.Now.AddSeconds(retryAfter));
                await Task.Delay(retryAfter * 1_000, cancellationToken);
                _logger
                    .ForContext("OperationToDd", operationToDo, true)
                    .ForContext("MessageQueue", messagesQueue, true)
                    .ForContext("RetryAfterSeconds", retryAfter)
                    .Warning("Delay because of 429 too many requests");

                messagesQueue.Enqueue(operationToDo);
                AddRemainingMessagesInQueueToBeProcessedAgain(messagesQueue);
            }
        } while (retryAgain);
    }

    private void AddRemainingMessagesInQueueToBeProcessedAgain(ConcurrentQueue<MessageDto>? queue)
    {
        if (queue is null) return;
        if (queue.IsEmpty) return;

        while (queue.TryDequeue(out var messageDto2))
        {
            _operationsQueueService.Add(messageDto2);
        }
    }

    private static MessageDto? GetOperationToDo(ConcurrentQueue<MessageDto> messagesQueue)
    {
        MessageDto? latestNew = null;
        MessageDto? latestUpdate = null;
        MessageDto? latestDelete = null;
        while (messagesQueue.TryDequeue(out var messageDto))
        {
            switch (messageDto.Type)
            {
                case MessageType.New:
                    latestNew ??= messageDto;
                    break;
                case MessageType.Update:
                    latestUpdate ??= messageDto;
                    break;
                case MessageType.Delete:
                    latestDelete ??= messageDto;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (latestNew is not null)
        {
            if (latestDelete is not null)
            {
                return null;
            }

            if (latestUpdate is not null)
            {
                latestUpdate.Type = MessageType.New;
                latestUpdate.MessageContains = MessageContains.TextMessage;
                latestUpdate.TextMessage = new TextMessage
                {
                    Text = latestUpdate.EditTextMessage.Text
                };

                return latestUpdate;
            }

            return latestNew;
        }

        if (latestUpdate is not null)
        {
            return latestDelete ?? latestUpdate;
        }

        return latestDelete;
    }

    private async Task HandleMessageAsync(ITelegramBotClient botClient, ICacheDbContext cacheDbContext, MessageDto messageDto)
    {
        using (LogContext.PushProperty("MessageId-GuidId", messageDto.GuidId))
        using (LogContext.PushProperty("ChatId", messageDto.ChatId))
        using (LogContext.PushProperty("MessageType", messageDto.Type))
        using (LogContext.PushProperty("MessageContains", messageDto.MessageContains))
        {
            switch (messageDto.Type)
            {
                case MessageType.New:
                    await HandleNewMessageAsync(botClient, cacheDbContext, messageDto);
                    break;
                case MessageType.Update:
                    await HandleSafeUpdateMessageAsync(botClient, messageDto);
                    break;
                case MessageType.Delete:
                    await HandleSafeDeleteMessageAsync(botClient, messageDto);
                    break;
                default:
                    _logger.Information("Out of range {@MessageDto}", messageDto);
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private async Task HandleNewMessageAsync(ITelegramBotClient botClient, ICacheDbContext cacheDbContext, MessageDto messageDto)
    {
        switch (messageDto.MessageContains)
        {
            case MessageContains.TextMessage:
                if (messageDto.TextMessage is null)
                {
                    throw new InvalidOperationException("Text message can not be null when sending new message");
                }

                var message = await botClient.SendTextMessageAsync(messageDto.ChatId,
                    messageDto.TextMessage);

                _memoryCache.Set(messageDto.GuidId, message.MessageId, DateTimeOffset.Now.AddHours(2));
                break;
            case MessageContains.GroupMedia:
                await SendGroupMediaAsync(botClient, cacheDbContext, messageDto);
                break;
            case MessageContains.Photo:
                if (messageDto.PhotoMessage is null)
                {
                    throw new InvalidOperationException("Photo message can not be null when sending new photo message");
                }

                var photoMessage = await SendPhotoAsync(botClient, messageDto.ChatId, messageDto.PhotoMessage);

                _memoryCache.Set(messageDto.GuidId, photoMessage.MessageId, DateTimeOffset.Now.AddHours(2));

                break;
            default:
                _logger.Information("Out of range {@MessageDto}", messageDto);
                throw new ArgumentOutOfRangeException();
        }
    }

    private static async Task<Message> SendPhotoAsync(ITelegramBotClient botClient, long chatId, PhotoMessageDto photoMessage)
    {
        var fileUri = FilePathToUriScheme(photoMessage.PhotoPath);

        return await botClient.SendPhotoAsync(chatId,
            fileUri,
            photoMessage.Caption,
            keyboard: photoMessage.ReplyMarkup);
    }

    private async Task SendGroupMediaAsync(ITelegramBotClient botClient, ICacheDbContext cacheDbContext, MessageDto messageDto)
    {
        var isCached = messageDto.IsCached;
        var isVideosNullOrEmpty = messageDto.Videos is null || messageDto.Videos.Count == 0;
        var isPhotosNullOrEmpty = messageDto.PhotosPath is null || messageDto.PhotosPath.Count == 0;
        var isAudioEmpty = messageDto.Audio is null;
        if (!isCached && isVideosNullOrEmpty && isPhotosNullOrEmpty && isAudioEmpty || messageDto.Caption is null)
        {
            throw new InvalidOperationException("Caption and audios and videos and photos cant be null or empty when message contains Group media");
        }

        try
        {
            if (messageDto.IsCached)
            {
                if (messageDto.CachedAudio is not null)
                {
                    await SendCachedAudioAsync(botClient, messageDto);
                }

                if (messageDto.CachedVideo is not null)
                {
                    await SendCachedVideoAsync(botClient, messageDto);
                }

                return;
            }

            var inputMedia = GetAlbumMedias(messageDto.PhotosPath,
                messageDto.Videos,
                messageDto.Caption);

            if (messageDto.Audio is not null)
            {
                var inputFile = InputFile.FromUri(FilePathToUriScheme(messageDto.Audio.AudioPath));
                InputFile? thumbnailFile = null;
                if (messageDto.Audio.ThumbnailPath is not null)
                {
                    thumbnailFile = InputFile.FromUri(FilePathToUriScheme(messageDto.Audio.ThumbnailPath));
                }

                var message = await botClient.SendAudioAsync(messageDto.ChatId,
                    inputFile,
                    caption: messageDto.Caption,
                    title: messageDto.Audio.AudioTitle,
                    duration: messageDto.Audio.DurationInSeconds,
                    thumbnail: thumbnailFile);

                if (messageDto.UserSentLink is not null && messageDto.UserSentLinkKey is not null)
                {
                    await _fileCacheService.CreateAudioCacheAsync(cacheDbContext,
                        message.Audio!,
                        messageDto.UserSentLink,
                        messageDto.UserSentLinkKey,
                        messageDto.Audio.Quality,
                        messageDto.Audio.ThumbnailPath);
                }
            }

            if (inputMedia.Count > 0)
            {
                var messages = await botClient.SendMediaGroupAsync(messageDto.ChatId, inputMedia);

                var videoMessage = messages.FirstOrDefault(x => x.Video is not null);
                if (messageDto.UserSentLink is not null && messageDto.UserSentLinkKey is not null && videoMessage is not null)
                {
                    var video = messageDto.Videos![0];
                    await _fileCacheService.CreateVideoCacheAsync(cacheDbContext,
                        videoMessage.Video!,
                        messageDto.UserSentLink,
                        messageDto.UserSentLinkKey,
                        (int)video.Duration.TotalSeconds,
                        video.Width ?? -1,
                        video.Height ?? -1,
                        video.ThumbnailPath);
                }
            }
        }
        finally
        {
            if (!isPhotosNullOrEmpty)
            {
                CleanUpPhotos();
            }

            if (!isVideosNullOrEmpty)
            {
                CleanUpVideos();
            }

            if (messageDto.Audio is not null)
            {
                File.Delete(messageDto.Audio.AudioPath);
                if (messageDto.Audio.ThumbnailPath is not null) File.Delete(messageDto.Audio.ThumbnailPath);
            }
        }

        void CleanUpPhotos()
        {
            foreach (var photoPath in messageDto.PhotosPath!)
            {
                File.Delete(photoPath);
            }
        }

        void CleanUpVideos()
        {
            foreach (var video in messageDto.Videos!)
            {
                File.Delete(video.VideoPath);
                if (video.ThumbnailPath is not null) File.Delete(video.ThumbnailPath);
            }
        }
    }

    private async Task SendCachedVideoAsync(ITelegramBotClient botClient, MessageDto messageDto)
    {
        if (messageDto.CachedVideo is null)
        {
            throw new InvalidOperationException("Can not send cached video when CachedVideo property is null");
        }

        var videoFile = InputFile.FromFileId(messageDto.CachedVideo.FileId);
        InputFile? thumbnailFile = null;
        if (messageDto.CachedVideo.ThumbnailPath is not null)
        {
            thumbnailFile = InputFile.FromUri(FilePathToUriScheme(messageDto.CachedVideo.ThumbnailPath));
        }

        await botClient.SendVideoAsync(messageDto.ChatId,
            videoFile,
            duration: messageDto.CachedVideo.Duration,
            width: messageDto.CachedVideo.Width,
            height: messageDto.CachedVideo.Height,
            thumbnail: thumbnailFile,
            supportsStreaming: true,
            caption: messageDto.Caption);
    }

    private async Task SendCachedAudioAsync(ITelegramBotClient botClient, MessageDto messageDto)
    {
        if (messageDto.CachedAudio is null)
        {
            throw new InvalidOperationException("Can not send cached audio when CachedAudio property is null");
        }

        var audioFile = InputFile.FromFileId(messageDto.CachedAudio.FileId);
        InputFile? thumbnailFile = null;
        if (messageDto.CachedAudio.ThumbnailPath is not null)
        {
            thumbnailFile = InputFile.FromUri(FilePathToUriScheme(messageDto.CachedAudio.ThumbnailPath));
        }

        await botClient.SendAudioAsync(messageDto.ChatId,
            audioFile,
            caption: messageDto.Caption,
            title: messageDto.CachedAudio.Title,
            duration: messageDto.CachedAudio.Duration,
            thumbnail: thumbnailFile);
    }

    private static List<IAlbumInputMedia> GetAlbumMedias(List<string>? photosPath, List<VideoDto>? videos, string albumCaption)
    {
        var albumMedia = new List<IAlbumInputMedia>();
        var isCaptionSet = false;
        foreach (var photoPath in photosPath ?? Enumerable.Empty<string>())
        {
            albumMedia.Add(CreatePhotoInputMedia(photoPath));
        }

        foreach (var video in videos ?? Enumerable.Empty<VideoDto>())
        {
            albumMedia.Add(CreateVideoInputMedia(video));
        }

        return albumMedia;

        InputMediaPhoto CreatePhotoInputMedia(string filePath)
        {
            var file = InputFile.FromUri(FilePathToUriScheme(filePath));
            if (isCaptionSet) return new InputMediaPhoto(file);
            isCaptionSet = true;
            return new InputMediaPhoto(file)
            {
                Caption = albumCaption,
            };
        }

        InputMediaVideo CreateVideoInputMedia(VideoDto video)
        {
            InputFileUrl? thumbnail = null;
            if (video.ThumbnailPath is not null)
            {
                thumbnail = InputFile.FromUri(FilePathToUriScheme(video.ThumbnailPath));
            }

            if (isCaptionSet)
                return new InputMediaVideo(InputFile.FromUri(FilePathToUriScheme(video.VideoPath)))
                {
                    Height = video.Height,
                    Width = video.Width,
                    Duration = (int)video.Duration.TotalSeconds,
                    Thumbnail = thumbnail,
                    SupportsStreaming = true
                };

            isCaptionSet = true;
            return new InputMediaVideo(InputFile.FromUri(FilePathToUriScheme(video.VideoPath)))
            {
                Caption = albumCaption,
                Height = video.Height,
                Width = video.Width,
                Duration = (int)video.Duration.TotalSeconds,
                Thumbnail = thumbnail,
                SupportsStreaming = true
            };
        }
    }

    private static Uri FilePathToUriScheme(string filePath)
    {
        if (!Path.IsPathRooted(filePath))
        {
            return new Uri("file://" + filePath);
        }

        return new Uri("file:/" + filePath);
    }

    private async Task HandleSafeUpdateMessageAsync(ITelegramBotClient botClient, MessageDto messageDto)
    {
        try
        {
            switch (messageDto.MessageContains)
            {
                case MessageContains.EditTextMessage:
                    if (!_memoryCache.TryGetValue<int>(messageDto.GuidId, out var messageId))
                    {
                        _logger
                            .ForContext("GuidIdRequested", messageDto.GuidId)
                            .ForContext("MessageDto", messageDto, true)
                            .Information("Messsage guid not found");
                        throw new InvalidOperationException("Message guid is not stored in cache to get messageId");
                    }

                    if (messageDto.EditTextMessage is null)
                    {
                        _logger
                            .ForContext("MessageDto", messageDto, true)
                            .Information("Edit text message is null");
                        throw new InvalidOperationException("Edit text message can not be null when editing message");
                    }

                    var message = await botClient.EditMessageTextAsync(messageDto.ChatId, messageId, messageDto.EditTextMessage);
                    _memoryCache.Set(messageDto.GuidId, message.MessageId);
                    break;
                default:
                    _logger.Information("Out of range {@MessageDto}", messageDto);
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException e) when (e.ErrorCode == 400 && e.Message.StartsWith("Bad Request: message is not modified"))
        {
            // Error is : Bad Request: message is not modified: specified new message content and reply markup are exactly the same as a current content and reply markup of the message
            // ignore
        }
    }

    private async Task HandleSafeDeleteMessageAsync(ITelegramBotClient botClient, MessageDto messageDto)
    {
        if (!_memoryCache.TryGetValue<int>(messageDto.GuidId, out var messageId))
        {
            _logger
                .ForContext("MessageDto", messageDto, true)
                .Information("Safe delete, message id not found in cache");
            return;
        }

        try
        {
            await botClient.DeleteMessageAsync(chatId: messageDto.ChatId, messageId: messageId);
            _memoryCache.Remove(messageDto.GuidId);
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException e) when (e.Message == "Bad Request: message to delete not found")
        {
            // Ignore
        }
    }

    protected abstract string GenerateCacheKeyForRetryAfter(long chatId);

    private bool TryGetRetryAfter(long chatId, [MaybeNullWhen(false)] out DateTimeOffset? retryAfter)
    {
        var key = GenerateCacheKeyForRetryAfter(chatId);
        return _memoryCache.TryGetValue(key, out retryAfter);
    }

    private void SetRetryAfter(long chatId, DateTimeOffset retryAfter)
    {
        var key = GenerateCacheKeyForRetryAfter(chatId);
        _memoryCache.Set(key, retryAfter);
    }

    private void RemoveRetryAfterIfExists(long chatId)
    {
        var key = GenerateCacheKeyForRetryAfter(chatId);
        _memoryCache.Remove(key);
    }
}