using Instagram69bot.Downloader.DTOs;
using RabbitMQ.Client;
using Serilog;

namespace Instagram69bot.Downloader.Shared.Publisher;

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