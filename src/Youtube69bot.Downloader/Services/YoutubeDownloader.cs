using Serilog;
using Serilog.Context;
using SerilogTimings;
using Youtube69bot.Downloader.Dapper;

namespace Youtube69bot.Downloader.Services;

public class YoutubeDownloader
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly TelegramUploader _telegramUploader;
    private readonly DownloadMetricsService _downloadMetricsService;
    private readonly TelegramMessageService _telegramMessageService;

    public YoutubeDownloader(IHttpClientFactory httpClientFactory, TelegramUploader telegramUploader, DownloadMetricsService downloadMetricsService, TelegramMessageService telegramMessageService)
    {
        _httpClientFactory = httpClientFactory;
        _telegramUploader = telegramUploader;
        _downloadMetricsService = downloadMetricsService;
        _telegramMessageService = telegramMessageService;
        _logger = Log.Logger.ForContext<YoutubeDownloader>();
    }

    public async Task DownloadAsync(string? youtubeLink,
        IEnumerable<string> collectionLinks,
        string? thumbnailLink,
        string? title,
        int totalLinksCount,
        int totalDownloadedCount,
        string messageId,
        long chatId,
        float? audioQuality,
        string userSentLink,
        CancellationToken cancellationToken = default)
    {
        _telegramMessageService.DeleteMessage(chatId, messageId);

        var downloadedCount = totalDownloadedCount;
        var filePaths = new List<string>();
        try
        {
            var albumCaption = CreateCaption();
            var lastCollectionStatusUpdateMessageId = _telegramMessageService.SendTextMessage(chatId, text: $"تعداد دانلود شده {downloadedCount} از {totalLinksCount}");


            var statusUpdateMessageId = _telegramMessageService.SendTextMessage(chatId: chatId, text: "در حال دریافت اطلاعات از سرور های یوتیوب");
            long totalFileSizeInBytes = 0;

            using (LogContext.PushProperty("DownloadCollectionCount", totalLinksCount))
            {
                using (Operation.Time("Download {NumberOfFiles} files", totalLinksCount))
                {
                    foreach (var linkDto in collectionLinks)
                    {
                        lastCollectionStatusUpdateMessageId = _telegramMessageService.EditMessageText(
                            chatId, lastCollectionStatusUpdateMessageId,
                            text: $"تعداد دانلود شده  {downloadedCount} از {totalLinksCount}");

                        _logger.Debug("Start file download");
                        var (filePath, isFileBiggerThan2Gb) = await DownloadFileAsync(linkDto, chatId, statusUpdateMessageId, cancellationToken);
                        _logger.Debug("Download file completed");

                        if (filePath is null && isFileBiggerThan2Gb.HasValue && isFileBiggerThan2Gb.Value)
                        {
                            _telegramMessageService.SendTextMessage(chatId: chatId,
                                text: "یکی از فایل های درخواست شده حجم بالاتر از 2 گیگابایت را دارد. به دلیل محدودیت های تنظیم شده از سمت تلگرام، امکان آپلود فایل میسر نمی باشد.");

                            if (File.Exists(filePath)) File.Delete(filePath);
                            downloadedCount++;
                            continue;
                        }

                        if (filePath is null) throw new InvalidOperationException("File path can not be null");
                        var fileInfo = new FileInfo(filePath);
                        totalFileSizeInBytes += fileInfo.Length;
                        var fileSizeMb = fileInfo.Length / 1024 / 1024;
                        if (fileSizeMb > 1998)
                        {
                            _telegramMessageService.SendTextMessage(chatId: chatId,
                                text: "یکی از فایل های درخواست شده حجم بالاتر از 2 گیگابایت را دارد. به دلیل محدودیت های تنظیم شده از سمت تلگرام، امکان آپلود فایل میسر نمی باشد.");

                            if (File.Exists(filePath)) File.Delete(filePath);
                            downloadedCount++;
                            continue;
                        }


                        filePaths.Add(filePath);
                        downloadedCount++;
                    }
                }

                _telegramMessageService.DeleteMessage(chatId, lastCollectionStatusUpdateMessageId);
                _telegramMessageService.DeleteMessage(chatId, statusUpdateMessageId);

                string? thumbnailPath = null;
                if (thumbnailLink is not null)
                {
                    _logger.Debug("Start thumbnail file download");
                    thumbnailPath = await DownloadFileWithoutReportAsync(thumbnailLink, cancellationToken);
                    _logger.Debug("Download thumbnail file completed");

                    using (Operation.Time("Resize and compress thumbnail for telegram"))
                    {
                        thumbnailPath = await ImageProcessor.ResizeToTelegramThumbnailSizeAndCompressAsync(thumbnailPath);
                    }
                }

                await _telegramUploader.UploadFilesToTelegramAsync(chatId,
                    filePaths,
                    albumCaption,
                    title,
                    thumbnailPath,
                    audioQuality,
                    userSentLink,
                    cancellationToken);

                await _downloadMetricsService.AddAsync(chatId, DownloadMetricsStatus.Completed,
                    totalFileSizeInBytes,
                    youtubeLink,
                    messageId);
            }
        }
        catch (Exception)
        {
            foreach (var filePath in filePaths.Where(File.Exists))
            {
                File.Delete(filePath);
            }

            throw;
        }
    }

    private async Task<string> DownloadFileWithoutReportAsync(string url, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient("proxy");

        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var contentType = response.Content.Headers.ContentType;

        var fileExtension = GetFileExtensionFromContentType(contentType!.MediaType!);
        var filePath = Path.Combine(Path.GetTempPath(), "saved-thumbnails/", $"{Guid.NewGuid()}{fileExtension}");
        await using var destinationStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

        try
        {
            await using var download = await response.Content.ReadAsStreamAsync(cancellationToken);
            await download.CopyToAsync(destinationStream, cancellationToken);

            return filePath;
        }
        catch (Exception)
        {
            File.Delete(filePath);
            throw;
        }
    }

    private async Task<(string? filePath, bool? isFileBiggerThan2Gb)> DownloadFileAsync(string url,
        long chatId,
        string statusUpdateMessageId,
        CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient("proxy");

        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var contentLength = response.Content.Headers.ContentLength;
        var contentType = response.Content.Headers.ContentType;

        var fileExtension = GetFileExtensionFromContentType(contentType!.MediaType!);
        var filePath = Path.Combine(Path.GetTempPath(), "saved-thumbnails/", $"{Guid.NewGuid()}{fileExtension}");
        await using var destinationStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

        try
        {
            await using var download = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (!contentLength.HasValue)
            {
                _telegramMessageService.EditMessageText(
                    chatId: chatId,
                    messageId: statusUpdateMessageId,
                    text: $"در حال دانلود پست مورد نظر. لطفا منتظر بمانید. ای دی {Guid.NewGuid().ToString()[..3]}");

                await download.CopyToAsync(destinationStream, cancellationToken);

                return (filePath, null);
            }

            var contentSizeMb = (decimal)contentLength / 1024 / 1024;
            switch (contentSizeMb)
            {
                case > 1998:
                    return (null, true);
                case < 3:
                    // Dont send update status when file size is small, it will be downloaded in 1-3s. dont need updates
                    // For some unknown reason, audios take a long time to download so i decreased the minimum size to not send
                    // status update to 3MB instead of 20MB
                    _telegramMessageService.EditMessageText(
                        chatId: chatId,
                        messageId: statusUpdateMessageId,
                        text: $"در حال دانلود پست مورد نظر. لطفا منتظر بمانید. ای دی {Guid.NewGuid().ToString()[..3]}");

                    await download.CopyToAsync(destinationStream, cancellationToken);

                    return (filePath, false);
            }

            var buffer = new byte[8192];
            long totalBytesRead = 0;
            int bytesRead;
            DateTime? nextStatusUpdateTime = null;
            decimal nextReportPercent = 0;
            while ((bytesRead = await download.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                var currentPercentage = GetProgressPercentage(totalBytesRead, contentLength.Value);
                currentPercentage = Math.Round(currentPercentage, MidpointRounding.ToZero);

                if (!ShouldUpdateStatus(nextStatusUpdateTime, nextReportPercent, currentPercentage)) continue;

                nextReportPercent = currentPercentage + Random.Shared.Next(5, 13);
                _telegramMessageService.EditMessageText(chatId,
                    statusUpdateMessageId,
                    $"ِدرصد دانلود شده: {currentPercentage}");

                nextStatusUpdateTime = DateTime.Now.AddSeconds(3.10);
            }

            return (filePath, false);
        }
        catch (Exception)
        {
            File.Delete(filePath);
            throw;
        }
    }

    private static bool ShouldUpdateStatus(DateTime? nextUpdateTime, decimal nextUpdatePercentage, decimal currentPercentage)
    {
        var isTheFirstUpdateStatus = nextUpdateTime is null;
        var isTimeForNextUpdate = DateTime.Now >= nextUpdateTime;
        var itsBeenSevenSecondsAfterTheLastUpdate = !isTheFirstUpdateStatus && DateTime.Now >= nextUpdateTime.Value.AddSeconds(7);

        if (!isTheFirstUpdateStatus && itsBeenSevenSecondsAfterTheLastUpdate) return true;


        if (isTheFirstUpdateStatus || isTimeForNextUpdate)
        {
            return currentPercentage >= nextUpdatePercentage;
        }

        return false;
    }

    private static string CreateCaption()
    {
        const string aloneTag = "Downloaded via @DownloadYoutube69_bot";
        return aloneTag;
    }

    private static decimal GetProgressPercentage(decimal totalBytes, decimal currentBytes)
    {
        return (totalBytes / currentBytes) * 100;
    }

    private string GetFileExtensionFromContentType(string contentType)
    {
        switch (contentType)
        {
            case "image/jpeg":
                return ".jpg";
            case "image/png":
                return ".png";
            case "image/gif":
                return ".gif";
            case "video/mp4":
                return ".mp4";
            case "video/quicktime":
                return ".mov";
            case "audio/mpeg":
                return ".mp3"; // https://github.com/ZiTAL/youtube-dl/blob/ac8ae6d32ebf8199ee11a89a03e5624460883116/mime.types#L529C1-L529C11
            case "audio/ogg":
                return ".ogg"; // https://github.com/ZiTAL/youtube-dl/blob/ac8ae6d32ebf8199ee11a89a03e5624460883116/mime.types#L529C1-L529C11
            case "audio/webm":
                return ".webm";
            case "audio/mp4":
                return ".m4a";
            default:
                _logger
                    .ForContext("ContentType", contentType)
                    .Warning("Unknown content type!");

                return string.Empty;
        }
    }
}