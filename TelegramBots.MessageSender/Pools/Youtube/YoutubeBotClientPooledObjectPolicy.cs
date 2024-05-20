using Microsoft.Extensions.ObjectPool;
using TelegramBots.MessageSender.DTOs.BotClientOptions;
using TelegramBots.MessageSender.Services.BotClients;

namespace TelegramBots.MessageSender.Pools;

public class YoutubeBotClientPooledObjectPolicy : PooledObjectPolicy<YoutubeTelegramBotClient>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly YoutubeBotClientOptions _telegramBotClientOptions;

    public YoutubeBotClientPooledObjectPolicy(IHttpClientFactory httpClientFactory, YoutubeBotClientOptions telegramBotClientOptions)
    {
        _httpClientFactory = httpClientFactory;
        _telegramBotClientOptions = telegramBotClientOptions;
    }

    /// <inheritdoc />
    public override YoutubeTelegramBotClient Create()
    {
        var httpClient =
#if DEBUG
            _httpClientFactory.CreateClient("proxy");
#else
                    _httpClientFactory.CreateClient();
#endif
        return new YoutubeTelegramBotClient(_telegramBotClientOptions, httpClient);
    }

    /// <inheritdoc />
    public override bool Return(YoutubeTelegramBotClient obj)
    {
        if (obj is IResettable resettable)
        {
            return resettable.TryReset();
        }

        return true;
    }
}