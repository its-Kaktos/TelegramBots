namespace Youtube69bot.Downloader.Shared;

public class RabbitMqConfig
{
    public required string ExchangeName { get; set; }
    public required string QueueName { get; set; }
    public required string RoutingKey { get; set; }
    public required string AppId { get; set; }
}

public class LinkResolverRabbitMqConfig : RabbitMqConfig
{
}

public class TelegramRabbitMqConfig : RabbitMqConfig
{
}