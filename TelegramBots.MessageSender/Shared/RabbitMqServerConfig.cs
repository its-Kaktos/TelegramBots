namespace TelegramBots.MessageSender.Shared;

public class RabbitMqServerConfig
{
    public required string HostName { get; set; }
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public int Port { get; set; }
    public ushort ConsumerDispatchConcurrency { get; set; }
    public ushort PrefetchCount { get; set; }
}