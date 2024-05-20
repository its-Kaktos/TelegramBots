using TelegramBots.MessageSender.DTOs.BotClientOptions;

namespace TelegramBots.MessageSender.Services.BotClients;

public class InstagramTelegramBotClient : BaseTelegramBotClient<InstagramBotClientOptions>
{
    public InstagramTelegramBotClient(InstagramBotClientOptions options, HttpClient? httpClient = default)
        : base(options, httpClient)
    {
    }
}