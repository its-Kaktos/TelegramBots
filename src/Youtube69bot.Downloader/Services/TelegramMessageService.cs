using Telegram.Bot.Types;
using Youtube69bot.Downloader.DTOs;
using Youtube69bot.Downloader.Shared.Publisher;

namespace Youtube69bot.Downloader.Services;

public class TelegramMessageService
{
    private readonly IRabbitMqProducer<TelegramMessageEvent> _telegramMessagePublisher;

    public TelegramMessageService(IRabbitMqProducer<TelegramMessageEvent> telegramMessagePublisher)
    {
        _telegramMessagePublisher = telegramMessagePublisher;
    }

    public string SendTextMessage(Message userMessage, string text, List<IEnumerable<TelegramKeyboardButton>>? replyMarkup = null)
    {
        return SendTextMessage(userMessage.Chat.Id, text, replyMarkup);
    }

    public string SendTextMessage(long chatId, string text, List<IEnumerable<TelegramKeyboardButton>>? replyMarkup = null)
    {
        var guid = Guid.NewGuid().ToString();
        var telegramEvent = new TelegramMessageEvent
        {
            Type = TelegramMessageType.New,
            AddedDate = DateTimeOffset.Now,
            ChatId = chatId,
            GuidId = guid,
            MessageContains = TelegramMessageContains.TextMessage,
            TextMessage = new TextMessageDto
            {
                Text = text,
                ReplyMarkup = replyMarkup
            }
        };
        _telegramMessagePublisher.Publish(telegramEvent);

        return guid;
    }

    public void DeleteMessage(long chatId, string messageId)
    {
        var telegramEvent = new TelegramMessageEvent
        {
            Type = TelegramMessageType.Delete,
            AddedDate = DateTimeOffset.Now,
            ChatId = chatId,
            GuidId = messageId,
            MessageContains = TelegramMessageContains.Nothing
        };
        _telegramMessagePublisher.Publish(telegramEvent);
    }

    public string EditMessageText(long chatId, string messageId, string text)
    {
        var telegramEvent = new TelegramMessageEvent
        {
            Type = TelegramMessageType.Update,
            AddedDate = DateTimeOffset.Now,
            ChatId = chatId,
            GuidId = messageId,
            MessageContains = TelegramMessageContains.EditTextMessage,
            EditTextMessage = new EditTextMessageDto()
            {
                Text = text
            }
        };
        _telegramMessagePublisher.Publish(telegramEvent);

        return messageId;
    }

    public string SendMediaGroup(long chatId, string caption, string userSentLink, List<string>? photosPath, List<VideoDto>? videos, AudioMessageDto? audio)
    {
        var guid = Guid.NewGuid().ToString();
        var telegramEvent = new TelegramMessageEvent
        {
            Type = TelegramMessageType.New,
            AddedDate = DateTimeOffset.Now,
            ChatId = chatId,
            GuidId = guid,
            MessageContains = TelegramMessageContains.GroupMedia,
            PhotosPath = photosPath,
            Videos = videos,
            Caption = caption,
            Audio = audio,
            UserSentLink = userSentLink,
            UserSentLinkKey = LinkKeyGenerator.Generate(userSentLink)
        };
        _telegramMessagePublisher.Publish(telegramEvent);

        return guid;
    }

    public string SendCachedMediaGroup(long chatId, string caption, string userSentLink, CachedAudio? audio, CachedVideo? video)
    {
        var guid = Guid.NewGuid().ToString();
        var telegramEvent = new TelegramMessageEvent
        {
            Type = TelegramMessageType.New,
            AddedDate = DateTimeOffset.Now,
            ChatId = chatId,
            GuidId = guid,
            MessageContains = TelegramMessageContains.GroupMedia,
            PhotosPath = null,
            Videos = null,
            Caption = caption,
            Audio = null,
            UserSentLink = userSentLink,
            UserSentLinkKey = LinkKeyGenerator.Generate(userSentLink),
            IsCached = true,
            CachedAudio = audio,
            CachedVideo = video
        };
        _telegramMessagePublisher.Publish(telegramEvent);

        return guid;
    }
}