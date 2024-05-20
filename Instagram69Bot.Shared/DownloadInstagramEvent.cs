namespace Instagram69Bot.Shared;

public class DownloadInstagramEvent
{
    public required string InstagramLink { get; set; }
    public required long TelegramChatId { get; set; }
    public required int TelegramMessageId { get; set; }
}