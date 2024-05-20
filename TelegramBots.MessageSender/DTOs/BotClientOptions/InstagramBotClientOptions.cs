using Telegram.Bot;

namespace TelegramBots.MessageSender.DTOs.BotClientOptions;

public class InstagramBotClientOptions : TelegramBotClientOptions
{
    public InstagramBotClientOptions(string token, string? baseUrl = null, bool useTestEnvironment = false)
        : base(token, baseUrl, useTestEnvironment)
    {
    }
}