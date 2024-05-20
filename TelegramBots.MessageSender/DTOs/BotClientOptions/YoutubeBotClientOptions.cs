using Telegram.Bot;

namespace TelegramBots.MessageSender.DTOs.BotClientOptions;

public class YoutubeBotClientOptions : TelegramBotClientOptions
{
    public YoutubeBotClientOptions(string token, string? baseUrl = null, bool useTestEnvironment = false)
        : base(token, baseUrl, useTestEnvironment)
    {
    }
}