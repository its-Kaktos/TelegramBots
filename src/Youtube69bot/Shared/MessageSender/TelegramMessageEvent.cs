namespace Youtube69bot.Shared.MessageSender;

public class TelegramMessageEvent
{
    public ApplicationName ApplicationName { set; get; } = ApplicationName.Youtube;
    public required long ChatId { get; set; }
    public required string GuidId { get; set; }
    public required DateTimeOffset AddedDate { get; set; }
    public required TelegramMessageType Type { get; set; }
    public required TelegramMessageContains MessageContains { get; set; }

    public TextMessageDto? TextMessage { get; set; }
    public EditTextMessageDto? EditTextMessage { get; set; }
    public PhotoMessageDto PhotoMessage { get; set; }
}

public class PhotoMessageDto
{
    public required string PhotoPath { get; set; }
    public string? Caption { get; set; }
    public List<IEnumerable<TelegramKeyboardButton>>? ReplyMarkup { get; set; }
}

public class TextMessageDto
{
    public required string Text { get; set; }
    public List<IEnumerable<TelegramKeyboardButton>>? ReplyMarkup { get; set; }
}

public class TelegramKeyboardButton
{
    public required string Text { get; set; }
    public string? CallbackData { get; set; }
    public string? Url { get; set; }
}

public class EditTextMessageDto
{
    public required string Text { get; set; }
}