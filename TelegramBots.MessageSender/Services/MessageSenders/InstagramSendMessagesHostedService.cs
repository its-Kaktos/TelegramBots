using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ObjectPool;
using Serilog;
using TelegramBots.MessageSender.Data;
using TelegramBots.MessageSender.DTOs.BotClientOptions;
using TelegramBots.MessageSender.Services.BotClients;
using TelegramBots.MessageSender.Services.Queues;

namespace TelegramBots.MessageSender.Services.MessageSenders;

public class InstagramSendMessagesHostedService : BaseMessagesHostedService<InstagramTelegramBotClient, InstagramBotClientOptions>
{
    private readonly ILogger _logger;
    private readonly InstagramOperationsQueueService _instagramOperationsQueueService;
    private readonly ObjectPool<InstagramTelegramBotClient> _telegramBotClientPool;
    private static readonly Type InstagramCacheDbContextType = typeof(InstagramCacheDbContext);

    public InstagramSendMessagesHostedService(ILogger logger, InstagramOperationsQueueService instagramOperationsQueueService,
        ObjectPool<InstagramTelegramBotClient> telegramBotClientPool, IMemoryCache memoryCache,
        FileCacheService fileCacheService, IServiceProvider serviceProvider)
        : base(logger, memoryCache, instagramOperationsQueueService, fileCacheService, serviceProvider)
    {
        _logger = logger;
        _instagramOperationsQueueService = instagramOperationsQueueService;
        _telegramBotClientPool = telegramBotClientPool;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Debug("Send instagram message host start");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var (chatId, processMessageDto) in _instagramOperationsQueueService.Operations)
                {
                    if (processMessageDto.MessagesGroup.IsEmpty)
                    {
                        _instagramOperationsQueueService.Operations.Remove(chatId, out _);
                        continue;
                    }

                    if (processMessageDto.IsProcessing)
                    {
                        continue;
                    }

                    processMessageDto.SetIsProcessing(true);
                    Task.Run(() => HandleMessageQueueForChatAsync(_telegramBotClientPool, InstagramCacheDbContextType, chatId, processMessageDto, stoppingToken), stoppingToken);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Exception occured while sending messages");
            }
            finally
            {
                await Task.Delay(50, stoppingToken);
            }
        }

        _logger.Information("Host stopped");
    }

    protected override string GenerateCacheKeyForRetryAfter(long chatId)
    {
        return "in_ra" + chatId;
    }
}