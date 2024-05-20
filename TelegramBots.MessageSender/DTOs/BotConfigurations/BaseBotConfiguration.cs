namespace TelegramBots.MessageSender.DTOs.BotConfigurations;

public abstract class BaseBotConfiguration
{
    public string BotToken { get; set; } = "";

    public string? BaseUrl { get; set; }
}