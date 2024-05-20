using Instagram69Bot.Abstract;
using Serilog;

namespace Instagram69Bot.Services;

// Compose Polling and ReceiverService implementations
public class PollingService : PollingServiceBase<ReceiverService>
{
    public PollingService(IServiceProvider serviceProvider, ILogger logger)
        : base(serviceProvider, logger)
    {
    }
}