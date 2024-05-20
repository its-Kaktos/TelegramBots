using Instagram69Bot.Dapper;
using Instagram69Bot.Data;
using Instagram69Bot.Services;
using Instagram69Bot.Shared;
using Instagram69Bot.Shared.Publisher;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using SerilogTimings;
using Telegram.Bot;
using Telegram.Bot.Types;
using User = Instagram69Bot.Data.User;

namespace Instagram69Bot.Commands.Instagram;

public class DownloadFromInstaCommand : BaseCommand
{
    private readonly IRabbitMqProducer<DownloadInstagramEvent> _publisher;
    private readonly ILogger _logger;
    private readonly DownloadMetricsService _downloadMetricsService;
    private readonly IMemoryCache _memoryCache;

    public DownloadFromInstaCommand(IRabbitMqProducer<DownloadInstagramEvent> publisher, ILogger logger, DownloadMetricsService downloadMetricsService, IMemoryCache memoryCache)
    {
        _publisher = publisher;
        _downloadMetricsService = downloadMetricsService;
        _memoryCache = memoryCache;
        _logger = logger.ForContext<DownloadFromInstaCommand>() ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleCommandAsync(TelegramMessageService telegramMessageService, ITelegramBotClient botClient, string instaLink, Message message, ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
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

        _publisher.Publish(new DownloadInstagramEvent
        {
            InstagramLink = instaLink,
            TelegramChatId = message.Chat.Id,
            TelegramMessageId = message.MessageId,
            ReplyMessageId = messageId
        });

        await _downloadMetricsService.AddAsync(message.Chat.Id,
            DownloadMetricsStatus.NewRequest,
            0,
            instaLink,
            message.MessageId);

        _logger.Debug("Published rabbitmq message successful");
    }

    private bool GetCanDownloadWithoutJoiningChannels(User user)
    {
        _memoryCache.TryGetValue<int>(user.UserId, out var userDownloadsWithoutJoiningChannelsCounter);

        // return userDownloadsWithoutJoiningChannelsCounter < 1;
        return false;
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