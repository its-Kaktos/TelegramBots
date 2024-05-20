using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Youtube69bot.Data;
using Youtube69bot.Shared;
using Youtube69bot.Shared.Publisher;

namespace Youtube69bot.Services;

public class DownloadLinkCallbackHandler
{
    private readonly TelegramMessageService _telegramMessageService;
    private readonly IRabbitMqProducer<YoutubeDownloadEvent> _publisher;

    public DownloadLinkCallbackHandler(TelegramMessageService telegramMessageService, IRabbitMqProducer<YoutubeDownloadEvent> publisher)
    {
        _telegramMessageService = telegramMessageService;
        _publisher = publisher;
    }

    public async Task HandleAsync(ApplicationDbContext dbContext, ITelegramBotClient botClient, string callbackQueryId, long chatId, string callbackData)
    {
        var linkDto = await dbContext.ResolvedYoutubeLinks.FirstOrDefaultAsync(x => x.Id == callbackData);
        if (linkDto is null)
        {
            const string errorMessage = """
                                         لطفا دوباره لینک دانلود خود را برای ربات ارسال نمایید.
                                        """;

            _telegramMessageService.SendTextMessage(chatId, errorMessage);
            return;
        }

        _telegramMessageService.DeleteMessage(linkDto.TelegramChatId, linkDto.ReplyMessageId);

        await botClient.AnswerCallbackQueryAsync(callbackQueryId, "در حال پردازش...");

        var replyMessageId = _telegramMessageService.SendTextMessage(linkDto.TelegramChatId, "در حال پردازش...");

        _publisher.Publish(new YoutubeDownloadEvent
        {
            ReplyMessageId = replyMessageId,
            TelegramChatId = linkDto.TelegramChatId,
            TelegramMessageId = linkDto.TelegramMessageId,
            YoutubeLink = linkDto.YoutubeLink,
            DownloadLinks = new List<string>(1)
            {
                linkDto.DownloadLink
            },
            ThumbnailLink = linkDto.ThumbnailLink,
            Title = linkDto.Title,
            AudioQuality = linkDto.AudioQuality,
            VideoHeight = linkDto.VideoHeight,
            VideoWidth = linkDto.VideoWidth,
        });
    }
}