using Serilog;
using SerilogTimings;
using Youtube69bot.Downloader.DTOs;

namespace Youtube69bot.Downloader.Services;

public class TelegramUploader
{
    private readonly ILogger _logger;
    private readonly FFMpegWrapper _ffMpegWrapper;
    private readonly TelegramMessageService _telegramMessageService;

    public TelegramUploader(ILogger logger, FFMpegWrapper ffMpegWrapper, TelegramMessageService telegramMessageService)
    {
        _ffMpegWrapper = ffMpegWrapper;
        _telegramMessageService = telegramMessageService;
        _logger = logger.ForContext<TelegramUploader>();
    }

    public async Task UploadFilesToTelegramAsync(long chatId,
        List<string> filePaths,
        string albumCaption,
        string? title,
        string? thumbnailPath,
        float? audioQuality,
        string userSentLink,
        CancellationToken cancellationToken = default)
    {
        var uploadStatusMessageId = _telegramMessageService.SendTextMessage(chatId: chatId, text: "فایل ها با موفقیت دانلود شدند. در حال پردازش. به دلیل محدودیت های تلگرام، پردازش فایل ها ممکن است مقداری طول بکشد. لطفا صبور باشید.");

        foreach (var filePath in filePaths)
        {
            switch (Path.GetExtension(filePath))
            {
                case ".jpg":
                case ".png":
                    _telegramMessageService.SendMediaGroup(chatId, albumCaption, userSentLink, new List<string>(1)
                    {
                        filePath
                    }, null, null);
                    break;
                case ".gif":
                case ".mp4":
                case ".mov":
                    _telegramMessageService.SendMediaGroup(chatId, albumCaption, userSentLink, null, new List<VideoDto>(1)
                    {
                        await CreateVideoDtoAsync(filePath, thumbnailPath, cancellationToken)
                    }, null);
                    break;
                case ".mp3":
                case ".ogg":
                case ".m4a":
                case ".webm":
                    var (audioPath, audioDuration) = await _ffMpegWrapper.ToMp3FormatAsync(filePath);
                    File.Delete(filePath);
                    _telegramMessageService.SendMediaGroup(chatId, albumCaption, userSentLink, null, null, new AudioMessageDto
                    {
                        AudioPath = audioPath,
                        ThumbnailPath = thumbnailPath,
                        AudioTitle = title ?? Guid.NewGuid().ToString(),
                        DurationInSeconds = (int)audioDuration.TotalSeconds,
                        Quality = audioQuality!.Value
                    });
                    break;
                default:
                    _logger
                        .ForContext("FilePath", filePath)
                        .Warning("What is this extension?!");
                    break;
            }
        }


        _telegramMessageService.DeleteMessage(chatId, uploadStatusMessageId);
    }

    private async Task<VideoDto> CreateVideoDtoAsync(string filePath, string? thumbnailPath, CancellationToken ct)
    {
        var (duration, height, width) = await _ffMpegWrapper.GetVideoDataAsync(filePath, ct);

        if (thumbnailPath is null)
        {
            thumbnailPath = await _ffMpegWrapper.GetThumbnailAsync(filePath, height, width, ct);

            using (Operation.Time("Resize and compress thumbnail for telegram"))
            {
                thumbnailPath = await ImageProcessor.ResizeToTelegramThumbnailSizeAndCompressAsync(thumbnailPath);
            }
        }

        return new VideoDto
        {
            VideoPath = filePath,
            Duration = duration,
            Height = height,
            Width = width,
            ThumbnailPath = thumbnailPath
        };
    }
}