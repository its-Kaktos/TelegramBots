using Instagram69bot.Downloader.DTOs;
using Serilog;

namespace Instagram69bot.Downloader.Services;

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

    public async Task UploadFilesToTelegramAsync(long chatId, List<string> filePaths, string albumCaption,
        CancellationToken cancellationToken = default)
    {
        var uploadStatusMessageId = _telegramMessageService.SendTextMessage(chatId: chatId, text: "فایل ها با موفقیت دانلود شدند. در حال پردازش.");

        var (photos, videos) = await GetPhotosAndVideos(filePaths, cancellationToken);

        if (photos.Count > 0 || videos.Count > 0)
        {
            _telegramMessageService.EditMessageText(
                chatId: chatId,
                messageId: uploadStatusMessageId,
                text: "درحال ارسال به تلگرام");

            _telegramMessageService.SendMediaGroup(chatId, albumCaption, photos, videos);
        }

        _telegramMessageService.DeleteMessage(chatId, uploadStatusMessageId);
    }

    private async Task<(List<string> photosPath, List<VideoDto> videos)> GetPhotosAndVideos(List<string> filePaths, CancellationToken cancellationToken = default)
    {
        var photosPath = new List<string>();
        var videos = new List<VideoDto>();
        foreach (var filePath in filePaths)
        {
            switch (Path.GetExtension(filePath))
            {
                case ".jpg":
                case ".png":
                    photosPath.Add(filePath);
                    break;
                case ".gif":
                case ".mp4":
                case ".mov":
                    videos.Add(await CreateVideoDtoAsync(filePath, cancellationToken));
                    break;
                default:
                    _logger
                        .ForContext("FilePath", filePath)
                        .Warning("What is this extension?!");
                    break;
            }
        }

        return (photosPath, videos);

        async Task<VideoDto> CreateVideoDtoAsync(string filePath, CancellationToken ct)
        {
            var (duration, height, width) = await _ffMpegWrapper.GetVideoDataAsync(filePath, ct);

            // Thumbnail image width or height can not be greater than 320
            var newWidth = 320;
            var newHeight = (int)(height * ((double)newWidth / width));
            if (newHeight > 320)
            {
                newHeight = 320;
                newWidth = (int)(width * ((double)newHeight / height));
            }

            var thumbnailPath = await _ffMpegWrapper.GetThumbnailAsync(filePath, newHeight, newWidth, ct);
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
}