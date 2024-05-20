namespace Youtube69bot.Downloader.DTOs;

public class LinkResolverMessageDto
{
    public long TelegramChatId { get; set; }
    public int TelegramMessageId { get; set; }
    public required string ReplyMessageId { get; set; }
    public bool IsSuccessful { get; set; }
    public bool IsMediaNotFound { get; set; }
    public string? Exception { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionData { get; set; }
    public List<string> DownloadLinks { get; set; } = new();
    public string? YoutubeLink { get; set; }
    public string? ThumbnailLink { get; set; }
    public string? Title { get; set; }
    public int? VideoHeight { get; set; }
    public int? VideoWidth { get; set; }
    public float? AudioQuality { get; set; }
    public int? DurationInSeconds { get; set; }
}