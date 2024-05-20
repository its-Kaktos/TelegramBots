using Serilog;
using TelegramBots.MessageSender.DTOs;
using TelegramBots.MessageSender.Services.Queues;

namespace TelegramBots.MessageSender.Services;

public class MessageHandlerService
{
    private readonly ILogger _logger;
    private readonly InstagramOperationsQueueService _instagramOperationsQueueService;
    private readonly YoutubeOperationsQueueService _youtubeOperationsQueueService;

    public MessageHandlerService(ILogger logger, InstagramOperationsQueueService instagramOperationsQueueService, YoutubeOperationsQueueService youtubeOperationsQueueService)
    {
        _logger = logger.ForContext<MessageHandlerService>();
        _instagramOperationsQueueService = instagramOperationsQueueService;
        _youtubeOperationsQueueService = youtubeOperationsQueueService;
    }

    public void Handle(MessageDto messageDto)
    {
        _logger
            .ForContext("Message", messageDto, true)
            .Debug("Handling message");

        switch (messageDto.ApplicationName)
        {
            case ApplicationName.Instagram:
                _instagramOperationsQueueService.Add(messageDto);
                break;
            case ApplicationName.Youtube:
                _youtubeOperationsQueueService.Add(messageDto);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}