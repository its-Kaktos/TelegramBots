namespace Youtube69bot.Shared;

public class RabbitMqConfig
{
    public required string ExchangeName { get; set; }
    public required string QueueName { get; set; }
    public required string RoutingKey { get; set; }
    public required string AppId { get; set; }
}

public class YoutubeLinkResloverRabbitMqConfig : RabbitMqConfig
{
}

public class YoutubeDownloaderRabbitMqConfig : RabbitMqConfig
{
}

public class TelegramMessageSenderRabbitMqConfig : RabbitMqConfig
{
}

public class TelegramBotRabbitMqConfig : RabbitMqConfig
{
}