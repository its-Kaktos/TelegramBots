namespace Youtube69bot.Shared;

public class YoutubeDownloadEvent
{
    public long TelegramChatId { get; set; }
    public int TelegramMessageId { get; set; }
    public required string ReplyMessageId { get; set; }
    public List<string> DownloadLinks { get; set; } = new();
    public string? YoutubeLink { get; set; }

    public bool IsSuccessful { get; } = true;

    public bool IsMediaNotFound { get; } = false;

    public string? ThumbnailLink { get; set; }
    public string? Title { get; set; }
    public int? VideoHeight { get; set; }
    public int? VideoWidth { get; set; }
    public float? AudioQuality { get; set; }
}