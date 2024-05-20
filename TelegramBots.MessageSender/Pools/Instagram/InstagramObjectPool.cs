using System.Collections.Concurrent;
using Microsoft.Extensions.ObjectPool;
using Serilog;
using TelegramBots.MessageSender.Services.BotClients;

namespace TelegramBots.MessageSender.Pools.Instagram;

public class InstagramObjectPool : ObjectPool<InstagramTelegramBotClient>
{
    private readonly InstagramBotClientPooledObjectPolicy _policy;
    private readonly int _maxCapacity;
    private int _numItems;

    private readonly ConcurrentQueue<InstagramTelegramBotClient> _items = new();
    private InstagramTelegramBotClient? _fastItem;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates an instance of <see cref="DefaultObjectPool{ApplicationTelegramBotClient}"/>.
    /// </summary>
    /// <param name="policy">The pooling policy to use.</param>
    /// <param name="maximumRetained">The maximum number of objects to retain in the pool.</param>
    public InstagramObjectPool(InstagramBotClientPooledObjectPolicy policy, int maximumRetained, IHttpClientFactory httpClientFactory, ILogger logger)
    {
        _policy = policy;
        _httpClientFactory = httpClientFactory;
        _logger = logger.ForContext<InstagramObjectPool>();
        // cache the target interface methods, to avoid interface lookup overhead
        _maxCapacity = maximumRetained - 1; // -1 to account for _fastItem
    }

    /// <inheritdoc />
    public override InstagramTelegramBotClient Get()
    {
        var item = _fastItem;
        if (item == null || Interlocked.CompareExchange(ref _fastItem, null, item) != item)
        {
            if (_items.TryDequeue(out item))
            {
                Interlocked.Decrement(ref _numItems);
                item.Client =
#if DEBUG
                    _httpClientFactory.CreateClient("proxy");
#else
                    _httpClientFactory.CreateClient();
#endif
                _logger.Debug("Got BotClient from queue");

                return item;
            }

            _logger.Debug("Create BotClient");

            // no object available, so go get a brand new one
            return _policy.Create();
        }

        item.Client =
#if DEBUG
            _httpClientFactory.CreateClient("proxy");
#else
                    _httpClientFactory.CreateClient();
#endif

        _logger.Debug("Got BotClient from fast item");
        return item;
    }

    /// <inheritdoc />
    public override void Return(InstagramTelegramBotClient obj)
    {
        ReturnCore(obj);
    }

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    /// <returns>true if the object was returned to the pool</returns>
    private bool ReturnCore(InstagramTelegramBotClient obj)
    {
        if (!_policy.Return(obj))
        {
            // policy says to drop this object
            return false;
        }

        if (_fastItem != null || Interlocked.CompareExchange(ref _fastItem, obj, null) != null)
        {
            if (Interlocked.Increment(ref _numItems) <= _maxCapacity)
            {
                _logger.Debug("Return BotClient, enqueue");
                _items.Enqueue(obj);
                return true;
            }

            _logger.Debug("Return BotClient, no room");
            // no room, clean up the count and drop the object on the floor
            Interlocked.Decrement(ref _numItems);
            return false;
        }

        return true;
    }
}