using Microsoft.Extensions.ObjectPool;
using TelegramBots.MessageSender.DTOs.BotClientOptions;
using TelegramBots.MessageSender.Services.BotClients;

namespace TelegramBots.MessageSender.Pools.Instagram;

public class InstagramBotClientPooledObjectPolicy : PooledObjectPolicy<InstagramTelegramBotClient>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly InstagramBotClientOptions _telegramBotClientOptions;

    public InstagramBotClientPooledObjectPolicy(IHttpClientFactory httpClientFactory, InstagramBotClientOptions telegramBotClientOptions)
    {
        _httpClientFactory = httpClientFactory;
        _telegramBotClientOptions = telegramBotClientOptions;
    }

    /// <inheritdoc />
    public override InstagramTelegramBotClient Create()
    {
        var httpClient =
#if DEBUG
            _httpClientFactory.CreateClient("proxy");
#else
                    _httpClientFactory.CreateClient();
#endif
        return new InstagramTelegramBotClient(_telegramBotClientOptions, httpClient);
    }

    /// <inheritdoc />
    public override bool Return(InstagramTelegramBotClient obj)
    {
        if (obj is IResettable resettable)
        {
            return resettable.TryReset();
        }

        return true;
    }
}