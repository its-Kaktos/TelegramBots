using System.Collections.Concurrent;
using Serilog;
using TelegramBots.MessageSender.DTOs;

namespace TelegramBots.MessageSender.Services.Queues;

public abstract class BaseOperationsQueueService
{
    private readonly ILogger _logger;

    public ConcurrentDictionary<long, ProcessMessageDto> Operations { get; } = new();

    internal BaseOperationsQueueService(ILogger logger)
    {
        _logger = logger.ForContext<BaseOperationsQueueService>();
    }

    /// <summary>Adds an object to the end of the Queue.</summary>
    /// <param name="operation">
    /// The object to add to the end of the Queue.
    /// </param>
    public void Add(MessageDto operation)
    {
        try
        {
            if (Operations.TryGetValue(operation.ChatId, out var processMessageDto))
            {
                if (processMessageDto.MessagesGroup.TryGetValue(operation.GuidId, out var messagesGroup))
                {
                    messagesGroup.Enqueue(operation);
                }

                var q = new ConcurrentQueue<MessageDto>();
                q.Enqueue(operation);
                processMessageDto.MessagesGroup.TryAdd(operation.GuidId, q);
                return;
            }

            var queue = new ConcurrentQueue<MessageDto>();
            queue.Enqueue(operation);

            var messagesGroupDict = new ConcurrentDictionary<string, ConcurrentQueue<MessageDto>>();
            messagesGroupDict.TryAdd(operation.GuidId, queue);

            Operations.TryAdd(operation.ChatId, new ProcessMessageDto
            {
                MessagesGroup = messagesGroupDict
            });
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error occured while adding Operation to queue");
        }
    }
}