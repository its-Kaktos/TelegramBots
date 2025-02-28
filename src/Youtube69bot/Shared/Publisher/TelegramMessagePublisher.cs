using RabbitMQ.Client;
using Serilog;
using Youtube69bot.Shared.MessageSender;

namespace Youtube69bot.Shared.Publisher;

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