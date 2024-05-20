namespace Instagram69bot.Downloader.DTOs;

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
    public List<string> CollectionLinks { get; set; } = new();
    public string? InstagramLink { get; set; }
}