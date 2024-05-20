using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBots.MessageSender.DTOs;

namespace TelegramBots.MessageSender.TelegramBotClientExtensions;

public static class TelegramBotSenderExtensions
{
    private static IReplyMarkup? GetReplyMarkup(List<List<TelegramKeyboardButton>> telegramKeyboard)
    {
        if (telegramKeyboard.Count <= 0) return null;

        var keyboard = new List<IEnumerable<InlineKeyboardButton>>();
        foreach (var keyboardButtons in telegramKeyboard)
        {
            var buttonRow = new List<InlineKeyboardButton>();
            foreach (var button in keyboardButtons)
            {
                if (button.Url is not null)
                {
                    buttonRow.Add(
                        InlineKeyboardButton.WithUrl(
                            text: button.Text,
                            url: button.Url));
                    continue;
                }

                buttonRow.Add(InlineKeyboardButton.WithCallbackData(text: button.Text, callbackData: button.CallbackData));
            }

            keyboard.Add(buttonRow);
        }

        return new InlineKeyboardMarkup(keyboard);
    }

    public static async Task<Message> SendTextMessageAsync(this ITelegramBotClient botClient, long chatId, TextMessage textMessage,
        CancellationToken cancellationToken = default)
    {
        var replyMarkUp = textMessage.ReplyMarkup is null ? null : GetReplyMarkup(textMessage.ReplyMarkup);

        return await botClient.SendTextMessageAsync(chatId,
            textMessage.Text,
            replyMarkup: replyMarkUp,
            cancellationToken: cancellationToken);
    }

    public static async Task<Message> EditMessageTextAsync(this ITelegramBotClient botClient,
        ChatId chatId,
        int messageId,
        EditTextMessage editTextMessage,
        CancellationToken cancellationToken = default)
    {
        return await botClient.EditMessageTextAsync(chatId,
            messageId,
            editTextMessage.Text,
            cancellationToken: cancellationToken);
    }

    public static async Task<Message> SendPhotoAsync(this ITelegramBotClient botClient,
        long chatId,
        Uri fileUri,
        string? caption,
        List<List<TelegramKeyboardButton>>? keyboard = null,
        CancellationToken cancellationToken = default)
    {
        var replyMarkUp = keyboard is null ? null : GetReplyMarkup(keyboard);

        return await botClient.SendPhotoAsync(chatId,
            photo: InputFile.FromUri(fileUri),
            caption: caption,
            replyMarkup: replyMarkUp,
            cancellationToken: cancellationToken);
    }
}

public static class TelegramBotClientExtensions
{
    private const int DelayMs = 100;

    public static async Task<Message> SafeSendPhotoAsync(this ITelegramBotClient botClient,
        ChatId chatId,
        InputFile photo,
        int? messageThreadId = default,
        string? caption = default,
        ParseMode? parseMode = default,
        IEnumerable<MessageEntity>? captionEntities = default,
        bool? hasSpoiler = default,
        bool? disableNotification = default,
        bool? protectContent = default,
        int? replyToMessageId = default,
        bool? allowSendingWithoutReply = default,
        IReplyMarkup? replyMarkup = default,
        CancellationToken cancellationToken = default)
    {
        bool isSuccess;
        do
        {
            try
            {
                var result = await botClient.SendPhotoAsync(chatId,
                    photo,
                    messageThreadId,
                    caption,
                    parseMode,
                    captionEntities,
                    hasSpoiler,
                    disableNotification,
                    protectContent,
                    replyToMessageId,
                    allowSendingWithoutReply,
                    replyMarkup,
                    cancellationToken: cancellationToken);

                await Task.Delay(DelayMs, cancellationToken);
                return result;
            }
            catch (ApiRequestException e) when (e.Parameters?.RetryAfter is not null)
            {
                await Task.Delay(e.Parameters.RetryAfter.Value * 1_000, cancellationToken);
                isSuccess = false;
            }
        } while (!isSuccess);

        throw new UnreachableException();
    }

    public static async Task<Message> SafeSendAudioAsync(this ITelegramBotClient botClient,
        ChatId chatId,
        InputFile audio,
        int? messageThreadId = default,
        string? caption = default,
        ParseMode? parseMode = default,
        IEnumerable<MessageEntity>? captionEntities = default,
        int? duration = default,
        string? performer = default,
        string? title = default,
        InputFile? thumbnail = default,
        bool? disableNotification = default,
        bool? protectContent = default,
        int? replyToMessageId = default,
        bool? allowSendingWithoutReply = default,
        IReplyMarkup? replyMarkup = default,
        CancellationToken cancellationToken = default)
    {
        bool isSuccess;
        do
        {
            try
            {
                var result = await botClient.SendAudioAsync(chatId,
                    audio,
                    messageThreadId,
                    caption,
                    parseMode,
                    captionEntities,
                    duration,
                    performer,
                    title,
                    thumbnail,
                    disableNotification,
                    protectContent,
                    replyToMessageId,
                    allowSendingWithoutReply,
                    replyMarkup,
                    cancellationToken: cancellationToken);

                await Task.Delay(DelayMs, cancellationToken);
                return result;
            }
            catch (ApiRequestException e) when (e.Parameters?.RetryAfter is not null)
            {
                await Task.Delay(e.Parameters.RetryAfter.Value * 1_000, cancellationToken);
                isSuccess = false;
            }
        } while (!isSuccess);

        throw new UnreachableException();
    }

    public static async Task<Message> SafeSendVideoAsync(this ITelegramBotClient botClient,
        ChatId chatId,
        InputFile video,
        int? messageThreadId = default,
        int? duration = default,
        int? width = default,
        int? height = default,
        InputFile? thumbnail = default,
        string? caption = default,
        ParseMode? parseMode = default,
        IEnumerable<MessageEntity>? captionEntities = default,
        bool? hasSpoiler = default,
        bool? supportsStreaming = default,
        bool? disableNotification = default,
        bool? protectContent = default,
        int? replyToMessageId = default,
        bool? allowSendingWithoutReply = default,
        IReplyMarkup? replyMarkup = default,
        CancellationToken cancellationToken = default)
    {
        bool isSuccess;
        do
        {
            try
            {
                var result = await botClient.SendVideoAsync(chatId,
                    video,
                    messageThreadId,
                    duration,
                    width,
                    height,
                    thumbnail,
                    caption,
                    parseMode,
                    captionEntities,
                    hasSpoiler,
                    supportsStreaming,
                    disableNotification,
                    protectContent,
                    replyToMessageId,
                    allowSendingWithoutReply,
                    replyMarkup,
                    cancellationToken: cancellationToken);

                await Task.Delay(DelayMs, cancellationToken);
                return result;
            }
            catch (ApiRequestException e) when (e.Parameters?.RetryAfter is not null)
            {
                await Task.Delay(e.Parameters.RetryAfter.Value * 1_000, cancellationToken);
                isSuccess = false;
            }
        } while (!isSuccess);

        throw new UnreachableException();
    }

    public static async Task<Message> SafeSendVoiceAsync(this ITelegramBotClient botClient,
        ChatId chatId,
        InputFile voice,
        int? messageThreadId = default,
        string? caption = default,
        ParseMode? parseMode = default,
        IEnumerable<MessageEntity>? captionEntities = default,
        int? duration = default,
        bool? disableNotification = default,
        bool? protectContent = default,
        int? replyToMessageId = default,
        bool? allowSendingWithoutReply = default,
        IReplyMarkup? replyMarkup = default,
        CancellationToken cancellationToken = default)
    {
        bool isSuccess;
        do
        {
            try
            {
                var result = await botClient.SendVoiceAsync(chatId,
                    voice,
                    messageThreadId,
                    caption,
                    parseMode,
                    captionEntities,
                    duration,
                    disableNotification,
                    protectContent,
                    replyToMessageId,
                    allowSendingWithoutReply,
                    replyMarkup,
                    cancellationToken: cancellationToken);

                await Task.Delay(DelayMs, cancellationToken);
                return result;
            }
            catch (ApiRequestException e) when (e.Parameters?.RetryAfter is not null)
            {
                await Task.Delay(e.Parameters.RetryAfter.Value * 1_000, cancellationToken);
                isSuccess = false;
            }
        } while (!isSuccess);

        throw new UnreachableException();
    }

    public static async Task<Message> SafeSendDocumentAsync(this ITelegramBotClient botClient,
        ChatId chatId,
        InputFile document,
        int? messageThreadId = default,
        InputFile? thumbnail = default,
        string? caption = default,
        ParseMode? parseMode = default,
        IEnumerable<MessageEntity>? captionEntities = default,
        bool? disableContentTypeDetection = default,
        bool? disableNotification = default,
        bool? protectContent = default,
        int? replyToMessageId = default,
        bool? allowSendingWithoutReply = default,
        IReplyMarkup? replyMarkup = default,
        CancellationToken cancellationToken = default)
    {
        bool isSuccess;
        do
        {
            try
            {
                var result = await botClient.SendDocumentAsync(chatId,
                    document,
                    messageThreadId,
                    thumbnail,
                    caption,
                    parseMode,
                    captionEntities,
                    disableContentTypeDetection,
                    disableNotification,
                    protectContent,
                    replyToMessageId,
                    allowSendingWithoutReply,
                    replyMarkup,
                    cancellationToken: cancellationToken);

                await Task.Delay(DelayMs, cancellationToken);
                return result;
            }
            catch (ApiRequestException e) when (e.Parameters?.RetryAfter is not null)
            {
                await Task.Delay(e.Parameters.RetryAfter.Value * 1_000, cancellationToken);
                isSuccess = false;
            }
        } while (!isSuccess);

        throw new UnreachableException();
    }

    public static async Task<Message> SafeSendAnimationAsync(this ITelegramBotClient botClient,
        ChatId chatId,
        InputFile animation,
        int? messageThreadId = default,
        int? duration = default,
        int? width = default,
        int? height = default,
        InputFile? thumbnail = default,
        string? caption = default,
        ParseMode? parseMode = default,
        IEnumerable<MessageEntity>? captionEntities = default,
        bool? hasSpoiler = default,
        bool? disableNotification = default,
        bool? protectContent = default,
        int? replyToMessageId = default,
        bool? allowSendingWithoutReply = default,
        IReplyMarkup? replyMarkup = default,
        CancellationToken cancellationToken = default)
    {
        bool isSuccess;
        do
        {
            try
            {
                var result = await botClient.SendAnimationAsync(chatId,
                    animation,
                    messageThreadId,
                    duration,
                    width,
                    height,
                    thumbnail,
                    caption,
                    parseMode,
                    captionEntities,
                    hasSpoiler,
                    disableNotification,
                    protectContent,
                    replyToMessageId,
                    allowSendingWithoutReply,
                    replyMarkup,
                    cancellationToken: cancellationToken);

                await Task.Delay(DelayMs, cancellationToken);
                return result;
            }
            catch (ApiRequestException e) when (e.Parameters?.RetryAfter is not null)
            {
                await Task.Delay(e.Parameters.RetryAfter.Value * 1_000, cancellationToken);
                isSuccess = false;
            }
        } while (!isSuccess);

        throw new UnreachableException();
    }

    public static async Task<Message[]> SafeSendMediaGroupAsync(this ITelegramBotClient botClient,
        ChatId chatId,
        IEnumerable<IAlbumInputMedia> media,
        int? messageThreadId = default,
        bool? disableNotification = default,
        bool? protectContent = default,
        int? replyToMessageId = default,
        bool? allowSendingWithoutReply = default,
        CancellationToken cancellationToken = default)
    {
        bool isSuccess;
        do
        {
            try
            {
                var result = await botClient.SendMediaGroupAsync(chatId,
                    media,
                    messageThreadId,
                    disableNotification,
                    protectContent,
                    replyToMessageId,
                    allowSendingWithoutReply,
                    cancellationToken: cancellationToken);

                await Task.Delay(DelayMs * 5, cancellationToken);
                return result;
            }
            catch (ApiRequestException e) when (e.Parameters?.RetryAfter is not null)
            {
                await Task.Delay(e.Parameters.RetryAfter.Value * 1_000, cancellationToken);
                isSuccess = false;
            }
        } while (!isSuccess);

        throw new UnreachableException();
    }
}