using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.ObjectPool;
using Serilog;
using TelegramBots.MessageSender.Data;
using TelegramBots.MessageSender.DTOs.BotClientOptions;
using TelegramBots.MessageSender.Services.BotClients;
using TelegramBots.MessageSender.Services.Queues;

namespace TelegramBots.MessageSender.Services.MessageSenders;

public class YoutubeSendMessagesHostedService : BaseMessagesHostedService<YoutubeTelegramBotClient, YoutubeBotClientOptions>
{
    private readonly ILogger _logger;
    private readonly YoutubeOperationsQueueService _youtubeOperationsQueueService;
    private readonly ObjectPool<YoutubeTelegramBotClient> _telegramBotClientPool;
    private static readonly Type YoutubeDbContextType = typeof(YoutubeCacheDbContext);

    public YoutubeSendMessagesHostedService(ILogger logger, YoutubeOperationsQueueService youtubeOperationsQueueService,
        ObjectPool<YoutubeTelegramBotClient> telegramBotClientPool, IMemoryCache memoryCache,
        FileCacheService fileCacheService, IServiceProvider serviceProvider)
        : base(logger, memoryCache, youtubeOperationsQueueService, fileCacheService, serviceProvider)
    {
        _logger = logger;
        _youtubeOperationsQueueService = youtubeOperationsQueueService;
        _telegramBotClientPool = telegramBotClientPool;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Debug("Send youtube message host start");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var (chatId, processMessageDto) in _youtubeOperationsQueueService.Operations)
                {
                    if (processMessageDto.MessagesGroup.IsEmpty)
                    {
                        _youtubeOperationsQueueService.Operations.Remove(chatId, out _);
                        continue;
                    }

                    if (processMessageDto.IsProcessing)
                    {
                        continue;
                    }

                    processMessageDto.SetIsProcessing(true);
                    Task.Run(() => HandleMessageQueueForChatAsync(_telegramBotClientPool, YoutubeDbContextType, chatId, processMessageDto, stoppingToken), stoppingToken);
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
        return "yt_ra" + chatId;
    }
}