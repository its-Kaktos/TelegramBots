using Instagram69Bot.Shared;
using Instagram69Bot.Shared.Publisher;
using RabbitMQ.Client;
using Serilog;

namespace Instagram69Bot.Publisher
{
    public class DownloadInstagramPublisher : ProducerBase<DownloadInstagramEvent>
    {
        public DownloadInstagramPublisher(
            ConnectionFactory connectionFactory,
            ILogger logger,
            RabbitMqConfig rabbitMqConfig) :
            base(connectionFactory, logger, rabbitMqConfig)
        {
        }
    }
}