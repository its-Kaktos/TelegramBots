using Serilog;
using Serilog.Context;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace Instagram69Bot.Abstract;

/// <summary>
/// An abstract class to compose Receiver Service and Update Handler classes
/// </summary>
/// <typeparam name="TUpdateHandler">Update Handler to use in Update Receiver</typeparam>
public abstract class ReceiverServiceBase<TUpdateHandler> : IReceiverService
    where TUpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUpdateHandler _updateHandler;
    private readonly ILogger _logger;

    internal ReceiverServiceBase(ITelegramBotClient botClient, TUpdateHandler updateHandler, ILogger logger)
    {
        _botClient = botClient;
        _updateHandler = updateHandler;
        _logger = logger.ForContext<ReceiverServiceBase<TUpdateHandler>>() ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Start to service Updates with provided Update Handler class
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    public async Task ReceiveAsync(CancellationToken stoppingToken)
    {
        // ToDo: we can inject ReceiverOptions through IOptions container
        var receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = Array.Empty<UpdateType>(),
            ThrowPendingUpdates = true,
        };

        var me = await _botClient.GetMeAsync(stoppingToken);
        using (LogContext.PushProperty("Bot name", me.Username ?? "Unknown username"))
        {
            _logger.Information("Start receiving updates");

            // Start receiving updates
            await _botClient.ReceiveAsync(
                updateHandler: _updateHandler,
                receiverOptions: receiverOptions,
                cancellationToken: stoppingToken);
        }
    }
}