using Telegram.Bot.Types;
using Youtube69bot.Shared.MessageSender;
using Youtube69bot.Shared.Publisher;

namespace Youtube69bot.Services;

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
            ApplicationName = ApplicationName.Youtube,
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

    public string SendPhotoMessage(long chatId, string photoPath, string? caption = null, List<IEnumerable<TelegramKeyboardButton>>? replyMarkup = null)
    {
        var guid = Guid.NewGuid().ToString();
        var telegramEvent = new TelegramMessageEvent
        {
            ApplicationName = ApplicationName.Youtube,
            Type = TelegramMessageType.New,
            AddedDate = DateTimeOffset.Now,
            ChatId = chatId,
            GuidId = guid,
            MessageContains = TelegramMessageContains.Photo,
            PhotoMessage = new PhotoMessageDto
            {
                PhotoPath = photoPath,
                Caption = caption,
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
            ApplicationName = ApplicationName.Youtube,
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
            ApplicationName = ApplicationName.Youtube,
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
}