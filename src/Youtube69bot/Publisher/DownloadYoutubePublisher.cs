using RabbitMQ.Client;
using Serilog;
using Youtube69bot.Shared;
using Youtube69bot.Shared.Publisher;

namespace Youtube69bot.Publisher
{
    public class DownloadYoutubePublisher : ProducerBase<YoutubeLinkResolveEvent>
    {
        public DownloadYoutubePublisher(
            ConnectionFactory connectionFactory,
            ILogger logger,
            RabbitMqConfig rabbitMqConfig) :
            base(connectionFactory, logger, rabbitMqConfig)
        {
        }
    }
}