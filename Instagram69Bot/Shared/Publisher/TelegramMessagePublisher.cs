using Instagram69Bot.Shared.MessageSender;
using RabbitMQ.Client;
using Serilog;

namespace Instagram69Bot.Shared.Publisher;

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