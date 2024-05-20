using Microsoft.Extensions.ObjectPool;
using Serilog;
using TelegramBots.MessageSender.Services.BotClients;

namespace TelegramBots.MessageSender.Pools.Instagram;

public class InstagramObjectPoolProvider : ObjectPoolProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public InstagramObjectPoolProvider(IHttpClientFactory httpClientFactory, ILogger logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger.ForContext<InstagramObjectPoolProvider>();
    }

    /// <summary>
    /// The maximum number of objects to retain in the pool.
    /// </summary>
    public int MaximumRetained { get; set; } = Environment.ProcessorCount * 2;

    /// <inheritdoc/>
    public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        return new DefaultObjectPool<T>(policy, MaximumRetained);
    }

    /// <inheritdoc/>
    public ObjectPool<InstagramTelegramBotClient> Create(InstagramBotClientPooledObjectPolicy policy)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        return new InstagramObjectPool(policy, MaximumRetained, _httpClientFactory, _logger);
    }
}