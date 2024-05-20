using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot.Exceptions;
using Youtube69bot.Services;

namespace Youtube69bot.Abstract;

// A background service consuming a scoped service.
// See more: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services#consuming-a-scoped-service-in-a-background-task
/// <summary>
/// An abstract class to compose Polling background service and Receiver implementation classes
/// </summary>
/// <typeparam name="TReceiverService">Receiver implementation class</typeparam>
public abstract class PollingServiceBase<TReceiverService> : BackgroundService
    where TReceiverService : IReceiverService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    internal PollingServiceBase(IServiceProvider serviceProvider, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger.ForContext<PollingServiceBase<TReceiverService>>() ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Information("Starting polling service");

        Task.Run(() =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var bulkMessageSender = scope.ServiceProvider.GetRequiredService<BulkMessageSender>();

                // TODO Get userid from ENV?
                bulkMessageSender.SendRemainingBulkMessagesAsync(445759465).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Exception occured while re-sending bulk messages");
            }
        });
        await DoWork(stoppingToken);
    }

    private async Task DoWork(CancellationToken stoppingToken)
    {
        // Make sure we receive updates until Cancellation Requested,
        // no matter what errors our ReceiveAsync get
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Create new IServiceScope on each iteration.
                // This way we can leverage benefits of Scoped TReceiverService
                // and typed HttpClient - we'll grab "fresh" instance each time
                using var scope = _serviceProvider.CreateScope();
                var receiver = scope.ServiceProvider.GetRequiredService<TReceiverService>();

                await receiver.ReceiveAsync(stoppingToken);
            }
            catch (ApiRequestException e) when (e.Parameters?.RetryAfter is not null)
            {
                var retryAfter = e.Parameters.RetryAfter.Value;
                await Task.Delay(retryAfter * 1_000, stoppingToken);

                _logger
                    .ForContext("RetryAfterSeconds", retryAfter)
                    .Warning("Delay bot polling because of 429 too many requests");
            }
            // Update Handler only captures exception inside update polling loop
            // We'll catch all other exceptions here
            // see: https://github.com/TelegramBots/Telegram.Bot/issues/1106
            catch (Exception ex)
            {
                _logger.Error(ex, "Polling failed");

                // Cooldown if something goes wrong
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}