using Serilog;

namespace TelegramBots.MessageSender.Services.Queues;

public class YoutubeOperationsQueueService : BaseOperationsQueueService
{
    public YoutubeOperationsQueueService(ILogger logger) : base(logger)
    {
    }
}