using Microsoft.Extensions.Caching.Memory;
using Serilog;
using SerilogTimings;
using Telegram.Bot;
using Telegram.Bot.Types;
using Youtube69bot.Dapper;
using Youtube69bot.Data;
using Youtube69bot.Services;
using Youtube69bot.Shared;
using Youtube69bot.Shared.Publisher;
using User = Youtube69bot.Data.User;

namespace Youtube69bot.Commands;

public class DownloadFromYoutubeCommand : BaseCommand
{
    private readonly IRabbitMqProducer<YoutubeLinkResolveEvent> _publisher;
    private readonly ILogger _logger;
    private readonly DownloadMetricsService _downloadMetricsService;
    private readonly IMemoryCache _memoryCache;

    public DownloadFromYoutubeCommand(IRabbitMqProducer<YoutubeLinkResolveEvent> publisher, ILogger logger, DownloadMetricsService downloadMetricsService, IMemoryCache memoryCache)
    {
        _publisher = publisher;
        _downloadMetricsService = downloadMetricsService;
        _memoryCache = memoryCache;
        _logger = logger.ForContext<DownloadFromYoutubeCommand>() ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleCommandAsync(TelegramMessageService telegramMessageService, ITelegramBotClient botClient, string youtubeLink, Message message, ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var user = await GetOrCreateUserAsync(dbContext, message.Chat.Id, message.From!.Id, cancellationToken);

        bool isUserInChannels;
        using (Operation.Time("Checking user is joined in channels"))
        {
            isUserInChannels = await IsUserStillJoinedInChannelsAsync(botClient, dbContext, user, cancellationToken);
        }

        if (!isUserInChannels)
        {
            if (!GetCanDownloadWithoutJoiningChannels(user))
            {
                await SetUserJoinedChannelsToFalseAsync(dbContext, user, cancellationToken);
                await SendUserMandatoryChannelsToJoinAsync(telegramMessageService, dbContext, user, cancellationToken);
                return;
            }

            IncreaseDownloadCountOfNotJoinedUser(user);
        }

        var messageId = telegramMessageService.SendTextMessage(message, "در حال اضافه کردن درخواست شما در صف دانلود");

        _publisher.Publish(new YoutubeLinkResolveEvent
        {
            YoutubeLink = youtubeLink,
            TelegramChatId = message.Chat.Id,
            TelegramMessageId = message.MessageId,
            ReplyMessageId = messageId
        });

        await _downloadMetricsService.AddAsync(message.Chat.Id,
            DownloadMetricsStatus.NewRequest,
            0,
            youtubeLink,
            message.MessageId);

        _logger.Debug("Published rabbitmq message successful");
    }

    private bool GetCanDownloadWithoutJoiningChannels(User user)
    {
        _memoryCache.TryGetValue<int>(user.UserId, out var userDownloadsWithoutJoiningChannelsCounter);

        return userDownloadsWithoutJoiningChannelsCounter < 1;
    }

    private void IncreaseDownloadCountOfNotJoinedUser(User user)
    {
        if (_memoryCache.TryGetValue<int>(user.UserId, out var userDownloadsWithoutJoiningChannelsCounter))
        {
            userDownloadsWithoutJoiningChannelsCounter++;
        }

        _memoryCache.Set(user.UserId, userDownloadsWithoutJoiningChannelsCounter, TimeSpan.FromHours(24));
    }
}