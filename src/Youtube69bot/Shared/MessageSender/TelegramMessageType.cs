namespace Youtube69bot.Shared.MessageSender;

public enum TelegramMessageType
{
    New,
    Update,
    Delete
}

public enum TelegramMessageContains
{
    Nothing,
    TextMessage,
    EditTextMessage,
    GroupMedia,
    Photo
}