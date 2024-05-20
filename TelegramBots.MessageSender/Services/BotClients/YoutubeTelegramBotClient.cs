using TelegramBots.MessageSender.DTOs.BotClientOptions;

namespace TelegramBots.MessageSender.Services.BotClients;

public class YoutubeTelegramBotClient : BaseTelegramBotClient<YoutubeBotClientOptions>
{
    public YoutubeTelegramBotClient(YoutubeBotClientOptions options, HttpClient? httpClient = default)
        : base(options, httpClient)
    {
    }
}