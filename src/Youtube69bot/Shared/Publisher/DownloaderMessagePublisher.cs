using RabbitMQ.Client;
using Serilog;

namespace Youtube69bot.Shared.Publisher;

public class DownloaderMessagePublisher : ProducerBase<YoutubeDownloadEvent>
{
    public DownloaderMessagePublisher(
        ConnectionFactory connectionFactory,
        ILogger logger,
        RabbitMqConfig rabbitMqConfig) :
        base(connectionFactory, logger, rabbitMqConfig)
    {
    }
}