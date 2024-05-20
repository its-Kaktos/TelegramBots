using Serilog;
using Youtube69bot.Abstract;

namespace Youtube69bot.Services;

// Compose Polling and ReceiverService implementations
public class PollingService : PollingServiceBase<ReceiverService>
{
    public PollingService(IServiceProvider serviceProvider, ILogger logger)
        : base(serviceProvider, logger)
    {
    }
}