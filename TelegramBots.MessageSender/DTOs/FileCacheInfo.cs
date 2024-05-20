using System.ComponentModel.DataAnnotations;

namespace TelegramBots.MessageSender.DTOs;

public class FileCacheInfo
{
    public long Id { get; set; }

    [MaxLength(750)]
    public required string UserSentLink { get; set; }

    [MaxLength(750)]
    public required string UserSentLinkKey { get; set; }

    public FileCacheType Type { get; set; }
    public List<VideoCache> Videos { get; set; } = new();
    public List<AudioCache> Audios { get; set; } = new();
}

public enum FileCacheType
{
    Video = 0,
    Audio = 1,
    VideosAndAudios = 2
}

public class VideoCache
{
    public long Id { get; set; }

    [MaxLength(256)]
    public required string FileId { get; set; }

    public long FileCacheInfoId { get; set; }
    public FileCacheInfo FileCacheInfo { get; set; }

    [MaxLength(750)]
    public string? ThumbnailPath { get; set; }

    public int Height { get; set; }
    public int Width { get; set; }
    public int Duration { get; set; }
}

public class AudioCache
{
    public long Id { get; set; }

    [MaxLength(256)]
    public required string FileId { get; set; }

    public long FileCacheInfoId { get; set; }
    public FileCacheInfo FileCacheInfo { get; set; }

    [MaxLength(750)]
    public required string Title { get; set; }

    [MaxLength(750)]
    public string? ThumbnailPath { get; set; }

    public int Duration { get; set; }
    public float Quality { get; set; }
}