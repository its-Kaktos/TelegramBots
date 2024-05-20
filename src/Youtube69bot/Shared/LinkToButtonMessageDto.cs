namespace Youtube69bot.Shared;

public class LinkToButtonMessageDto
{
    public required string YoutubeLink { get; set; }
    public long TelegramChatId { get; set; }
    public int TelegramMessageId { get; set; }
    public required string ReplyMessageId { get; set; }
    public bool IsSuccessful { get; set; }
    public bool IsMediaNotFound { get; set; }
    public List<LinkToButtonAudioMessageDto> Audios { get; set; } = new();
    public List<LinkToButtonVideoMessageDto> VideosWithSound { get; set; } = new();
    public string? ThumbnailLink { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Exception { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionData { get; set; }

    // Highest viewed video on youtube has 10 billion views!
    public long? ViewsCount { get; set; }
    public int? DurationInSeconds { get; set; }
    public string? YoutubeErrorMessage { get; set; }
}

public class LinkToButtonAudioMessageDto
{
    public required float Quality { get; set; }
    public required string DownloadLink { get; set; }
    public int? FileSize { get; set; }
    public required string Extension { get; set; }
}

public class LinkToButtonVideoMessageDto
{
    public required string Quality { get; set; }
    public required string DownloadLink { get; set; }
    public int? FileSize { get; set; }
    public required string Extension { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
}