using System.Text.RegularExpressions;
using Instagram69bot.Downloader.Dapper;
using Serilog;
using Serilog.Context;
using SerilogTimings;

namespace Instagram69bot.Downloader.Services;

public class InstagramDownloader
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly TelegramUploader _telegramUploader;
    private readonly DownloadMetricsService _downloadMetricsService;
    private static Regex ReplaceUrlHost = new("^(https?://)([^/]+)(.*)$", RegexOptions.Compiled);
    private readonly TelegramMessageService _telegramMessageService;

    public InstagramDownloader(IHttpClientFactory httpClientFactory, TelegramUploader telegramUploader, DownloadMetricsService downloadMetricsService, TelegramMessageService telegramMessageService)
    {
        _httpClientFactory = httpClientFactory;
        _telegramUploader = telegramUploader;
        _downloadMetricsService = downloadMetricsService;
        _telegramMessageService = telegramMessageService;
        _logger = Log.Logger.ForContext<InstagramDownloader>();
    }

    public async Task DownloadAsync(string? instagramLink,
        IEnumerable<string> collectionLinks,
        int totalLinksCount,
        int totalDownloadedCount,
        string messageId,
        long chatId,
        CancellationToken cancellationToken = default)
    {
        _telegramMessageService.DeleteMessage(chatId, messageId);

        var downloadedCount = totalDownloadedCount;
        var filePaths = new List<string>();
        try
        {
            var albumCaption = CreateCaption();
            var lastCollectionStatusUpdateMessageId = _telegramMessageService.SendTextMessage(chatId, text: $"تعداد دانلود شده {downloadedCount} از {totalLinksCount}");


            var statusUpdateMessageId = _telegramMessageService.SendTextMessage(chatId: chatId, text: "در حال دریافت اطلاعات از سرور های اینستاگرام");
            long totalFileSizeInBytes = 0;

            using (LogContext.PushProperty("InstagramCollectionCount", totalLinksCount))
            {
                using (Operation.Time("Download {NumberOfFiles} files", totalLinksCount))
                {
                    foreach (var linkDto in collectionLinks)
                    {
                        lastCollectionStatusUpdateMessageId = _telegramMessageService.EditMessageText(
                            chatId, lastCollectionStatusUpdateMessageId,
                            text: $"تعداد دانلود شده  {downloadedCount} از {totalLinksCount}");

                        _logger.Debug("Start file download");
                        var filePath = await DownloadFileAsync(linkDto, chatId, statusUpdateMessageId, cancellationToken);
                        _logger.Debug("Download file completed");

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

                await _telegramUploader.UploadFilesToTelegramAsync(chatId, filePaths, albumCaption, cancellationToken);

                await _downloadMetricsService.AddAsync(chatId, DownloadMetricsStatus.Completed,
                    totalFileSizeInBytes,
                    instagramLink,
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

    private async Task<string> DownloadFileAsync(string url,
        long chatId,
        string statusUpdateMessageId,
        CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient("proxy");

        const string replacement = "$1" + "scontent.cdninstagram.com" + "$3";
        url = ReplaceUrlHost.Replace(url, replacement);

        _logger.Debug("New url is {NewUrl}", url);

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

                return filePath;
            }

            var contentSizeMb = (float)contentLength / 1024 / 1024;
            // Dont send update status when file size is small, it will be downloaded in 1-3s. dont need updates
            if (contentSizeMb < 20)
            {
                _telegramMessageService.EditMessageText(
                    chatId: chatId,
                    messageId: statusUpdateMessageId,
                    text: $"در حال دانلود پست مورد نظر. لطفا منتظر بمانید. ای دی {Guid.NewGuid().ToString()[..3]}");

                await download.CopyToAsync(destinationStream, cancellationToken);

                return filePath;
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

            return filePath;
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
        var itsBeenSevenSecondsAfterTheLastUpdate = !isTheFirstUpdateStatus && DateTime.Now >= nextUpdateTime.Value.AddSeconds(3);

        if (!isTheFirstUpdateStatus && itsBeenSevenSecondsAfterTheLastUpdate) return true;


        if (isTheFirstUpdateStatus || isTimeForNextUpdate)
        {
            return currentPercentage >= nextUpdatePercentage;
        }

        return false;
    }

    private static string CreateCaption()
    {
        const string aloneTag = "Downloaded via @DownloadInstagram69_bot";
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
            default:
                _logger
                    .ForContext("ContentType", contentType)
                    .Warning("Unknown content type!");

                return string.Empty;
        }
    }
}