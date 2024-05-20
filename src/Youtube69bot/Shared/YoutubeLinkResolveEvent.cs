namespace Youtube69bot.Shared;

public class YoutubeLinkResolveEvent
{
    public required string YoutubeLink { get; set; }
    public required long TelegramChatId { get; set; }
    public required int TelegramMessageId { get; set; }
    public required string ReplyMessageId { get; set; }
}