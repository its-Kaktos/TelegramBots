using Serilog;
using Serilog.Context;
using Youtube69bot.Downloader.Dapper;
using Youtube69bot.Downloader.DTOs;

namespace Youtube69bot.Downloader.Services;

public class LinkResolverErrorHandler
{
    private readonly ILogger _logger;
    private readonly DownloadMetricsService _downloadMetricsService;
    private readonly TelegramMessageService _telegramMessageService;

    public LinkResolverErrorHandler(ILogger logger, DownloadMetricsService downloadMetricsService, TelegramMessageService telegramMessageService)
    {
        _downloadMetricsService = downloadMetricsService;
        _telegramMessageService = telegramMessageService;
        _logger = logger.ForContext<LinkResolverErrorHandler>() ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(LinkResolverMessageDto messageDto)
    {
        _telegramMessageService.DeleteMessage(messageDto.TelegramChatId, messageDto.ReplyMessageId!);

        await _downloadMetricsService.AddAsync(messageDto.TelegramChatId,
            DownloadMetricsStatus.Failed,
            0,
            messageDto.YoutubeLink,
            messageDto.TelegramMessageId);

        if (messageDto.IsMediaNotFound)
        {
            _telegramMessageService.SendTextMessage(messageDto.TelegramChatId,
                "پست ارسالی پیدا نشد، بررسی کن که لینک معتبر باشه و پیج پرایوت نباشه و اگر لینک استوری هست، استوری منقضی نشده باشه \n" +
                $"ای دی: {messageDto.TelegramMessageId} \n" +
                "اگر حس میکنی مشکلی پیش اومده با پشتیبانی در تماس باش");

            _logger.Debug("Media not found");
            return;
        }

        if (messageDto.Exception is null)
        {
            _telegramMessageService.SendTextMessage(messageDto.TelegramChatId,
                "متاسفانه سرور اینستاگرام در دسترس نیست، لطفا دوباره تلاش کنید. \n" +
                $"ای دی: {messageDto.TelegramMessageId} \n" +
                "اگر حس میکنی مشکلی پیش اومده با پشتیبانی در تماس باش");

            _logger.Debug("No exception occured and no media link has been found");
            return;
        }

        using (LogContext.PushProperty("Exception", messageDto.Exception))
        using (LogContext.PushProperty("ExceptionType", messageDto.ExceptionType))
        {
            var text = "متاسفانه خطایی رخ داد.\n" +
                       $"ای دی : {messageDto.TelegramMessageId}\n" +
                       "لینک ارسالی رو بررسی کن، اگر خطا باز رخ داد با پشتیبانی در تماس باش.";

            _telegramMessageService.SendTextMessage(
                chatId: messageDto.TelegramChatId,
                text: text);

            _logger.Error("Error occured in link resolver, exception data: {Exception}", messageDto.ExceptionData);
        }
    }
}