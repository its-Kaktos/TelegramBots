using Instagram69Bot.Abstract;
using Serilog;
using Telegram.Bot;

namespace Instagram69Bot.Services;

// Compose Receiver and UpdateHandler implementation
public class ReceiverService : ReceiverServiceBase<UpdateHandler>
{
    public ReceiverService(ITelegramBotClient botClient, UpdateHandler updateHandler, ILogger logger)
        : base(botClient, updateHandler, logger)
    {
    }
}