using System.ComponentModel.DataAnnotations;

namespace Youtube69bot.Data;

public class ResolvedYoutubeLink
{
    [MaxLength(450)]
    public required string Id { get; set; }

    public required string YoutubeLink { get; set; }
    public required long TelegramChatId { get; set; }
    public required int TelegramMessageId { get; set; }

    [MaxLength(450)]
    public required string ReplyMessageId { get; set; }

    public required DateTimeOffset AddedDate { get; set; }

    [MaxLength(1500)]
    public required string DownloadLink { get; set; }

    [MaxLength(450)]
    public string? ThumbnailLink { get; set; }

    [MaxLength(750)]
    public string? Title { get; set; }

    public int? VideoHeight { get; set; }
    public int? VideoWidth { get; set; }
    public float? AudioQuality { get; set; }
    public int? DurationInSeconds { get; set; }
}