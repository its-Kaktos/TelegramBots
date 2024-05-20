using Serilog;

namespace TelegramBots.MessageSender.Services.Queues;

public class InstagramOperationsQueueService : BaseOperationsQueueService
{
    public InstagramOperationsQueueService(ILogger logger) : base(logger)
    {
    }
}