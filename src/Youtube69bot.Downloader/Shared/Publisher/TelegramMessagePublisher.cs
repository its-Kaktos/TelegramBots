using RabbitMQ.Client;
using Serilog;
using Youtube69bot.Downloader.DTOs;

namespace Youtube69bot.Downloader.Shared.Publisher;

public class TelegramMessagePublisher : ProducerBase<TelegramMessageEvent>
{
    public TelegramMessagePublisher(
        ConnectionFactory connectionFactory,
        ILogger logger,
        RabbitMqConfig rabbitMqConfig) :
        base(connectionFactory, logger, rabbitMqConfig)
    {
    }
}