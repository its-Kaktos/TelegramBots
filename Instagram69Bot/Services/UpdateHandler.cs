using System.Text.RegularExpressions;
using Instagram69Bot.Commands;
using Instagram69Bot.Commands.Instagram;
using Instagram69Bot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Context;
using SerilogTimings;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Instagram69Bot.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger _logger;
    private readonly MainCommand _mainCommand;
    private readonly DownloadFromInstaCommand _downloadFromInstaCommand;
    private readonly AdminCommand _adminCommand;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly UserService _userService;
    private readonly TelegramMessageService _telegramMessageService;

    private static readonly Regex InstagramLinkRegex = new(@"^(?:https?:\/\/)?(?:www\.)?instagram\.com\/?([a-zA-Z0-9._-]+)?\/(?:p|reel|stories|tv)\/([a-zA-Z0-9-_.]+)\/?([0-9]+)?",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(750));

    public UpdateHandler(ITelegramBotClient botClient, ILogger logger, IServiceScopeFactory serviceScopeFactory, MainCommand mainCommand, DownloadFromInstaCommand downloadFromInstaCommand, AdminCommand adminCommand, UserService userService,
        TelegramMessageService telegramMessageService)
    {
        _botClient = botClient;
        _logger = logger.ForContext<UpdateHandler>() ?? throw new ArgumentNullException(nameof(logger));
        _serviceScopeFactory = serviceScopeFactory;
        _mainCommand = mainCommand;
        _downloadFromInstaCommand = downloadFromInstaCommand;
        _adminCommand = adminCommand;
        _userService = userService;
        _telegramMessageService = telegramMessageService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("UpdateType", update.Type))
        using (LogContext.PushProperty("UpdateId", update.Id))
        using (LogContext.PushProperty("MessageText", update.Message?.Text))
        using (LogContext.PushProperty("MessageId", update.Message?.MessageId))
        using (LogContext.PushProperty("UserId", update.Message?.From?.Id))
        using (LogContext.PushProperty("ChatId", update.Message?.Chat.Id))
        using (Operation.Time("Bot answering whole operation"))
        {
            _logger.Debug("Message received");
            var handler = update switch
            {
                // UpdateType.Unknown:
                // UpdateType.ShippingQuery:
                // UpdateType.PreCheckoutQuery:
                // UpdateType.Poll:
                { ChannelPost: { } _ } => Task.CompletedTask, // Ignore channel posts
                { EditedChannelPost: { } _ } => Task.CompletedTask, // Ignore channel posts
                { EditedMessage: { } _ } => Task.CompletedTask, // Ignore edited messages?
                { CallbackQuery: { } query } => BotOnCallbackQueryReceivedAsync(query, cancellationToken),
                { Message: { } message } => BotOnMessageReceivedAsync(message, cancellationToken),
                { MyChatMember: { } chatMember } => BotOnChatMemberUpdatedAsync(chatMember, cancellationToken),
                // { EditedMessage: { } message } => BotOnMessageReceivedAsync(message, cancellationToken),
                _ => UnknownUpdateHandlerAsync(update)
            };

            await handler;
        }
    }

    private async Task BotOnChatMemberUpdatedAsync(ChatMemberUpdated chatMember, CancellationToken cancellationToken)
    {
        _logger.Debug("Chat member updated, message {@ChatMemberUpdated}", chatMember);
        var dateEventHappened = new DateTimeOffset(chatMember.Date.ToUniversalTime());
        switch (chatMember.NewChatMember.Status)
        {
            case ChatMemberStatus.Member:
                await _userService.UnBlockedBotAsync(chatMember.Chat.Id, dateEventHappened, cancellationToken);
                break;
            case ChatMemberStatus.Kicked:
                await _userService.BlockedBotAsync(chatMember.Chat.Id, dateEventHappened, cancellationToken);
                break;
            default:
                _logger.Information("Unknown chat member update, update: {@ChatMemberUpdate}", chatMember);
                break;
        }
    }

    private async Task BotOnCallbackQueryReceivedAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        if (callbackQuery.Data is null) return;

        if (callbackQuery.Data == "check_user_joined_channel")
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = await dbContext.Users.AsNoTracking()
                .Where(u => u.UserId == callbackQuery.From.Id)
                .FirstAsync(cancellationToken: cancellationToken);

            if (user.IsInJoinedMandatoryChannels)
            {
                _logger.Debug("User is already joined mandatory channels");

                if (callbackQuery.Message?.Chat is not null)
                {
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id,
                        "عزیزدلم عضویتت تکمیل شده، برای استفاده از ربات کافیه لینک موردنظر رو برای ربات بفرستی",
                        cancellationToken: cancellationToken);

                    _telegramMessageService.SendTextMessage(callbackQuery.Message.Chat.Id,
                        "عزیزدلم عضویتت تکمیل شده، برای استفاده از ربات کافیه لینک موردنظر رو برای ربات بفرستی");

                    return;
                }

                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id,
                    "عزیزدلم عضویتت تکمیل شده، برای استفاده از ربات کافیه لینک موردنظر رو برای ربات بفرستی",
                    true, cancellationToken: cancellationToken);

                return;
            }

            var channelListUserNeedToJoin = await dbContext.Channels.AsNoTracking()
                .Where(c => c.VersionId == user.VersionUserJoinedId)
                .ToListAsync(cancellationToken: cancellationToken);

            using (LogContext.PushProperty("MandatoryChannelsVersion", user.VersionUserJoinedId))
            using (LogContext.PushProperty("MandatoryChannels", channelListUserNeedToJoin, true))
            {
                foreach (var channel in channelListUserNeedToJoin)
                {
                    var chatMember = await _botClient.GetChatMemberAsync(chatId: channel.ChannelId,
                        userId: callbackQuery.From!.Id,
                        cancellationToken: cancellationToken);

                    var isUserInChat = chatMember.Status switch
                    {
                        ChatMemberStatus.Creator or ChatMemberStatus.Administrator or
                            ChatMemberStatus.Member or ChatMemberStatus.Restricted => true,
                        _ => false
                    };

                    if (isUserInChat) continue;

                    _logger.Information("User is not joined in mandatory channels");

                    if (callbackQuery.Message?.Chat is not null)
                    {
                        await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id,
                            "هنوز جوین نشدی عزیزم :)",
                            cancellationToken: cancellationToken);

                        _telegramMessageService.SendTextMessage(callbackQuery.Message.Chat.Id, "هنوز جوین نشدی عزیزم :)");

                        return;
                    }

                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id,
                        "هنوز جوین نشدی عزیزم :)",
                        true, cancellationToken: cancellationToken);

                    return;
                }

                if (!user.IsInJoinedMandatoryChannels)
                {
                    var userJoinedChannels = new List<UserJoinedChannel>();

                    foreach (var channel in channelListUserNeedToJoin)
                    {
                        userJoinedChannels.Add(new UserJoinedChannel
                        {
                            ChannelId = channel.ChannelId,
                            UserChatId = user.ChatId,
                            JoinedDate = DateTimeOffset.Now
                        });
                    }

                    await dbContext.UserJoinedChannels.AddRangeAsync(userJoinedChannels, cancellationToken);
                }

                user.IsInJoinedMandatoryChannels = true;
                dbContext.Users.Update(user);
                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.Debug("User joined all mandatory channels");

                if (callbackQuery.Message?.Chat is not null)
                {
                    await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id,
                        "عزیزدلم عضویتت تکمیل شده، برای استفاده از ربات کافیه لینک موردنظر رو برای ربات بفرستی",
                        cancellationToken: cancellationToken);

                    _telegramMessageService.SendTextMessage(callbackQuery.Message.Chat.Id,
                        "عزیزدلم عضویتت تکمیل شده، برای استفاده از ربات کافیه لینک موردنظر رو برای ربات بفرستی");

                    return;
                }

                await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id,
                    "عزیزدلم عضویتت تکمیل شده، برای استفاده از ربات کافیه لینک موردنظر رو برای ربات بفرستی",
                    true, cancellationToken: cancellationToken);
            }
        }
    }

    private async Task BotOnMessageReceivedAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.Text is not { } messageText) return;

        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        switch (messageText)
        {
            case "/start":
                _logger.Debug("Main command handler, handling message");
                await _mainCommand.HandleCommandAsync(_botClient, dbContext, _telegramMessageService, message, cancellationToken);
                break;
            case "ping":
                _logger.Debug("Ping command handler, handling message");
                _telegramMessageService.SendTextMessage(message, "Pong 🏓");
                break;
            case var _ when messageText.StartsWith("/admin"):
                await _adminCommand.HandleCommandAsync(_botClient, dbContext, _telegramMessageService, message, cancellationToken);
                break;
            default:
                await HandleUnknownCommandAsync(_telegramMessageService, dbContext, _botClient, message, cancellationToken);
                break;
        }
    }

    private void AnswerUpdating(TelegramMessageService telegramMessageService, Message message)
    {
        const string errorMessage = """
                                    سلام دوست من.
                                    در حال آپدیت ربات و انتقال سرور هستیم.
                                    پس از پایان انتقال بهتون داخل چنل زیر اطلاع خواهیم داد
                                    ممنون بابت حمایتتون
                                    @i69bots
                                    """;

        telegramMessageService.SendTextMessage(message, errorMessage);
    }

    private async Task HandleUnknownCommandAsync(TelegramMessageService telegramMessageService, ApplicationDbContext dbContext, ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        const string errorMessage = "عزیز دل، بررسی کن که لینک ارسالیت مشکلی نداشته باشه، از پیج پرایوت نباشه و شبیه لینک زیر باشه:\n" +
                                    "https://www.instagram.com/p/CyAdBdSrqTT/?igshid=MzRlODBiNWFlZA==\n" +
                                    "اگر فکر میکنی مشکلی پیش اومده یا ربات کار نمیکنه، با پشتیبانی در تماس باش.";
        using (LogContext.PushProperty("InstagramLink", message.Text))
        {
            if (message.Text is null || message.Text.Length > 300)
            {
                _logger.Warning("Message text is null or its greater than 300");

                telegramMessageService.SendTextMessage(message, errorMessage);
            }

            var regexMatch = InstagramLinkRegex.Match(message.Text!);

            if (!regexMatch.Success)
            {
                _logger.Debug("Provided link is not valid");

                telegramMessageService.SendTextMessage(message, errorMessage);

                return;
            }

            _logger.Debug("Provided message is a valid Instagram link");
            await _downloadFromInstaCommand.HandleCommandAsync(telegramMessageService, botClient, regexMatch.Value, message, dbContext, cancellationToken);
        }
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.ForContext("UpdateType", update.Type)
            .Warning("Unknown update type, update: {@Update}", update);

        return Task.CompletedTask;
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.Error(exception, "Polling handler error");

        // Cooldown in case of network connection error
        if (exception is RequestException)
        {
            const int seconds = 2;
            _logger.Information("Cooling down polling for {Seconds} seconds because of network error", seconds);
            await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
        }
    }
}