namespace Instagram69bot.Downloader.DTOs;

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
    GroupMedia
}