using Serilog;
using Telegram.Bot;
using Youtube69bot.Abstract;

namespace Youtube69bot.Services;

// Compose Receiver and UpdateHandler implementation
public class ReceiverService : ReceiverServiceBase<UpdateHandler>
{
    public ReceiverService(ITelegramBotClient botClient, UpdateHandler updateHandler, ILogger logger)
        : base(botClient, updateHandler, logger)
    {
    }
}