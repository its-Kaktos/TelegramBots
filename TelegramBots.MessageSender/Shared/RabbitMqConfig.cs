namespace TelegramBots.MessageSender.Shared;

public class RabbitMqConfig
{
    public required string ExchangeName { get; set; }
    public required string QueueName { get; set; }
    public required string RoutingKey { get; set; }
    public required string AppId { get; set; }
}