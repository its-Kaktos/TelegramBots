using Instagram69Bot.Shared.MessageSender;
using Instagram69Bot.Shared.Publisher;
using Telegram.Bot.Types;

namespace Instagram69Bot.Services;

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
}