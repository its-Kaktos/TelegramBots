namespace TelegramBots.MessageSender.DTOs;

public class MessageDto
{
    public required ApplicationName ApplicationName { get; set; }
    public required long ChatId { get; set; }
    public required string GuidId { get; set; }
    public required DateTimeOffset AddedDate { get; set; }
    public required MessageType Type { get; set; }
    public required MessageContains MessageContains { get; set; }
    public TextMessage? TextMessage { get; set; }
    public EditTextMessage? EditTextMessage { get; set; }
    public PhotoMessageDto? PhotoMessage { get; set; }
    public List<string>? PhotosPath { get; set; }
    public List<VideoDto>? Videos { get; set; }
    public string? Caption { get; set; }
    public AudioMessageDto? Audio { get; set; }
    public string? UserSentLink { get; set; }
    public string? UserSentLinkKey { get; set; }
    public bool IsCached { get; set; } = false;
    public CachedAudio? CachedAudio { get; set; }
    public CachedVideo? CachedVideo { get; set; }
}

public class CachedVideo
{
    public required string FileId { get; set; }
    public string? ThumbnailPath { get; set; }
    public int Duration { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class CachedAudio
{
    public required string FileId { get; set; }
    public required string Title { get; set; }
    public string? ThumbnailPath { get; set; }
    public int Duration { get; set; }
    public float Quality { get; set; }
}

public class AudioMessageDto
{
    public required string AudioPath { get; set; }
    public string? ThumbnailPath { get; set; }
    public required string AudioTitle { get; set; }
    public int DurationInSeconds { get; set; }
    public float Quality { get; set; }
}

public class TextMessage
{
    public required string Text { get; set; }
    public List<List<TelegramKeyboardButton>>? ReplyMarkup { get; set; }
}

public class PhotoMessageDto
{
    public required string PhotoPath { get; set; }
    public string? Caption { get; set; }
    public List<List<TelegramKeyboardButton>>? ReplyMarkup { get; set; }
}

public class TelegramKeyboardButton
{
    public required string Text { get; set; }
    public string? CallbackData { get; set; }
    public string? Url { get; set; }
}

public class EditTextMessage
{
    public required string Text { get; set; }
}

public class VideoDto
{
    public required string VideoPath { get; set; }
    public TimeSpan Duration { get; set; }
    public int? Height { get; set; }
    public int? Width { get; set; }
    public string? ThumbnailPath { get; set; }
}