using System.Collections.Concurrent;

namespace TelegramBots.MessageSender.DTOs;

public class ProcessMessageDto
{
    private readonly object _lock = new();
    public bool IsProcessing { get; private set; } = false;
    public required ConcurrentDictionary<string, ConcurrentQueue<MessageDto>> MessagesGroup { get; set; }

    public void SetIsProcessing(bool isProcessing)
    {
        lock (_lock)
        {
            IsProcessing = isProcessing;
        }
    }
}