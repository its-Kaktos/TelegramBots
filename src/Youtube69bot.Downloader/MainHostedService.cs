using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Serilog.Context;
using Youtube69bot.Downloader.Dapper;
using Youtube69bot.Downloader.DTOs;
using Youtube69bot.Downloader.Services;
using Youtube69bot.Downloader.Shared;

namespace Youtube69bot.Downloader;

public class MainHostedService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly LinkResolverRabbitMqConfig _rabbitMqConfig;
    private readonly RabbitMqServerConfig _rabbitMqServerConfig;
    private readonly IServiceProvider _serviceProvider;
    private readonly FileCacheService _fileCacheService;
    private readonly TelegramMessageService _telegramMessageService;

    public MainHostedService(ILogger logger, IServiceProvider serviceProvider, FileCacheService fileCacheService, TelegramMessageService telegramMessageService)
    {
        _logger = logger.ForContext<MainHostedService>() ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider;
        _fileCacheService = fileCacheService;
        _telegramMessageService = telegramMessageService;
        _rabbitMqConfig = serviceProvider.GetConfiguration<LinkResolverRabbitMqConfig>();
        _rabbitMqServerConfig = serviceProvider.GetConfiguration<RabbitMqServerConfig>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Debug("Starting application bot downloader");

        try
        {
            var factory = CreateFactory();

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            ConfigureChannel(channel);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                try
                {
                    MessageReceivedEventHandler(model, ea, stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Exception occured while processing rabbitmq message");
                    channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, false);

                    try
                    {
                        var body = ea.Body.ToArray();
                        var messageJson = Encoding.UTF8.GetString(body);
                        var messageDto = JsonSerializer.Deserialize<LinkResolverMessageDto>(messageJson);

                        using var scope = _serviceProvider.CreateScope();
                        var downloadMetricsService = scope.ServiceProvider.GetRequiredService<DownloadMetricsService>();
                        downloadMetricsService.AddAsync(messageDto!.TelegramChatId,
                                DownloadMetricsStatus.Failed,
                                0,
                                messageDto.YoutubeLink,
                                messageDto.TelegramMessageId)
                            .GetAwaiter().GetResult();
                    }
                    catch (Exception exception)
                    {
                        _logger.Error(exception, "Exception occured while storing metrics to DB");
                    }
                }
            };

            consumer.Shutdown += ShutdownEventHandler;

            channel.BasicConsume(queue: _rabbitMqConfig.QueueName,
                autoAck: false,
                consumer: consumer);


            // Keep app from closing.
            Console.ReadLine();
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.Debug("Working behind the scenes...");
                await Task.Delay(5000, stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
        catch (Exception e)
        {
            _logger.Fatal(e, "Host terminated unexpectedly");
            _logger.Information("rabbitmqConfig variables => {@conf}", _rabbitMqConfig);
            _logger.Information("rabbitmqServerConfig variables => {@conf}", _rabbitMqServerConfig);
            throw;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private ConnectionFactory CreateFactory()
    {
        return new ConnectionFactory
        {
            HostName = _rabbitMqServerConfig.HostName,
            UserName = _rabbitMqServerConfig.UserName,
            Password = _rabbitMqServerConfig.Password,
            Port = _rabbitMqServerConfig.Port, // set to -1 to use default ports
            ConsumerDispatchConcurrency = _rabbitMqServerConfig.ConsumerDispatchConcurrency, // CHECK THIS
        };
    }

    private void ConfigureChannel(IModel channel)
    {
        // Dont prefetch more than 1 data, see "Fair dispatch": https://www.rabbitmq.com/tutorials/tutorial-two-dotnet.html
        channel.BasicQos(prefetchSize: 0, prefetchCount: _rabbitMqServerConfig.ConsumerDispatchConcurrency, global: false);

        channel.ExchangeDeclare(_rabbitMqConfig.ExchangeName, ExchangeType.Direct, true);

        channel.QueueDeclare(queue: _rabbitMqConfig.QueueName, durable: true, exclusive: false, autoDelete: false);

        channel.QueueBind(_rabbitMqConfig.QueueName, _rabbitMqConfig.ExchangeName, _rabbitMqConfig.RoutingKey);
    }

    private void ShutdownEventHandler(object? sender, ShutdownEventArgs args)
    {
        _logger
            .ForContext("Cause", args.Cause)
            .ForContext("Initiator", args.Initiator)
            .ForContext("ReplyText", args.ReplyText)
            .Information("consumer closed??");
    }

    private void MessageReceivedEventHandler(object sender, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        _logger.Debug("Received message");
        using var scope = _serviceProvider.CreateScope();
        LinkResolverMessageDto? messageDto = null;
        var channel = ((EventingBasicConsumer)sender).Model;
        try
        {
            var body = ea.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);
            _logger.Debug("Message received, message: {message}", messageJson);

            messageDto = JsonSerializer.Deserialize<LinkResolverMessageDto>(messageJson);

            if (messageDto is null) throw new InvalidOperationException("Send message from rabbitmq is null.");
            var downloader = scope.ServiceProvider.GetRequiredService<YoutubeDownloader>();

            HandleDownloadingLink(messageDto, scope, downloader, messageDto.YoutubeLink, stoppingToken);
            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        }
        catch (SocketException e)
        {
            HandleSocketError(e, messageDto, scope, stoppingToken);
            channel.BasicNack(ea.DeliveryTag, false, false);
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException e)
        {
            HandleTelegramError(e, messageDto, scope, stoppingToken);
            channel.BasicNack(ea.DeliveryTag, false, false);
        }
        catch (Exception e)
        {
            HandlerMessageProcessingError(e, messageDto, scope, stoppingToken);
            channel.BasicNack(ea.DeliveryTag, false, false);
        }
    }

    private void HandleDownloadingLink(LinkResolverMessageDto messageDto,
        IServiceScope scope,
        YoutubeDownloader youtubeDownloader,
        string? youtubeLink,
        CancellationToken stoppingToken)
    {
        using (LogContext.PushProperty("MessageId", messageDto.TelegramMessageId))
        using (LogContext.PushProperty("ChatId", messageDto.TelegramChatId))
        using (LogContext.PushProperty("IsResolvingLinkSuccessful", messageDto.IsSuccessful))
        using (LogContext.PushProperty("YoutubeLink", messageDto.YoutubeLink))
        {
            if (!messageDto.IsSuccessful)
            {
                var errorHandler = scope.ServiceProvider.GetRequiredService<LinkResolverErrorHandler>();
                errorHandler.HandleAsync(messageDto)
                    .GetAwaiter().GetResult();

                return;
            }

            var youtubeCacheService = scope.ServiceProvider.GetRequiredService<YoutubeFileCacheService>();
            var isMessageSentByCache = messageDto.DownloadLinks.Count == 1 && _fileCacheService.TrySendFileByCacheAsync(youtubeCacheService, messageDto).GetAwaiter().GetResult();
            if (isMessageSentByCache)
            {
                _telegramMessageService.DeleteMessage(messageDto.TelegramChatId, messageDto.ReplyMessageId);

                _logger
                    .ForContext("YoutubeLink", messageDto.YoutubeLink)
                    .ForContext("AudioQuality", messageDto.AudioQuality)
                    .ForContext("VideoHeight", messageDto.VideoHeight)
                    .ForContext("VideoWidth", messageDto.VideoWidth)
                    .Information("Send file using cache");

                return;
            }

            const int processInBatchOf = 10;
            var downloadedCount = 0;
            for (var i = 0; i < messageDto.DownloadLinks.Count; i += processInBatchOf)
            {
                var linksToProcess = messageDto.DownloadLinks.Skip(i).Take(processInBatchOf);

                youtubeDownloader.DownloadAsync(youtubeLink,
                        linksToProcess,
                        messageDto.ThumbnailLink,
                        messageDto.Title,
                        messageDto.DownloadLinks.Count,
                        downloadedCount,
                        messageDto.ReplyMessageId,
                        messageDto.TelegramChatId,
                        messageDto.AudioQuality,
                        messageDto.YoutubeLink!,
                        stoppingToken)
                    .GetAwaiter().GetResult();

                downloadedCount += processInBatchOf;
                Task.Delay(250, stoppingToken).GetAwaiter().GetResult();
            }
        }
    }

    private void HandleSocketError(SocketException e, LinkResolverMessageDto? messageDto, IServiceScope scope, CancellationToken stoppingToken)
    {
        try
        {
            if (messageDto is not null)
            {
                var downloadMetricsService = scope.ServiceProvider.GetRequiredService<DownloadMetricsService>();
                downloadMetricsService.AddAsync(messageDto.TelegramChatId,
                        DownloadMetricsStatus.Failed,
                        0,
                        messageDto.YoutubeLink,
                        messageDto.TelegramMessageId)
                    .GetAwaiter().GetResult();

                var text = "🚧 مشکلی در هنگام پردازش درخواست شما پیش آمد \n" +
                           $"ای دی : {messageDto.TelegramMessageId}\n" +
                           "لطفا دوباره تلاش کنید.";

                _telegramMessageService.SendTextMessage(
                    chatId: messageDto.TelegramChatId,
                    text: text);
            }

            using (LogContext.PushProperty("MessageId", messageDto?.TelegramMessageId))
            using (LogContext.PushProperty("ChatId", messageDto?.TelegramChatId))
            using (LogContext.PushProperty("IsResolvingLinkSuccessful", messageDto?.IsSuccessful))
            using (LogContext.PushProperty("YoutubeLinks", messageDto?.YoutubeLink))
            using (LogContext.PushProperty("DownloadLinks", messageDto?.DownloadLinks))
            {
                _logger.Error(e, "Telegram exception occured while processing rabbitmq message");
            }
        }
        catch (Exception exception)
        {
            using (LogContext.PushProperty("MessageId", messageDto?.TelegramMessageId))
            using (LogContext.PushProperty("ChatId", messageDto?.TelegramChatId))
            using (LogContext.PushProperty("IsResolvingLinkSuccessful", messageDto?.IsSuccessful))
            using (LogContext.PushProperty("YoutubeLinks", messageDto?.YoutubeLink))
            using (LogContext.PushProperty("DownloadLinks", messageDto?.DownloadLinks))
            {
                _logger.Error(exception,
                    "Exception occured while processing exception of!! rabbitmq message, first EX: {@ex}", e);
            }
        }
    }

    private void HandleTelegramError(Exception e, LinkResolverMessageDto? messageDto, IServiceScope scope, CancellationToken stoppingToken)
    {
        try
        {
            if (messageDto is not null)
            {
                var downloadMetricsService = scope.ServiceProvider.GetRequiredService<DownloadMetricsService>();
                downloadMetricsService.AddAsync(messageDto.TelegramChatId,
                        DownloadMetricsStatus.Failed,
                        0,
                        messageDto.YoutubeLink,
                        messageDto.TelegramMessageId)
                    .GetAwaiter().GetResult();

                var text = "تلگرام: مشکلی در پردازش فایل ها پیش آمد.\n" +
                           $"ای دی : {messageDto.TelegramMessageId}\n" +
                           "به دلیل بروز خطا از سمت سرور های تلگرام، لطفا دوباره تلاش کنید.";

                _telegramMessageService.SendTextMessage(
                    chatId: messageDto.TelegramChatId,
                    text: text);
            }

            using (LogContext.PushProperty("MessageId", messageDto?.TelegramMessageId))
            using (LogContext.PushProperty("ChatId", messageDto?.TelegramChatId))
            using (LogContext.PushProperty("IsResolvingLinkSuccessful", messageDto?.IsSuccessful))
            using (LogContext.PushProperty("YoutubeLinks", messageDto?.YoutubeLink))
            using (LogContext.PushProperty("DownloadLinks", messageDto?.DownloadLinks))
            {
                _logger.Error(e, "Telegram exception occured while processing rabbitmq message");
            }
        }
        catch (Exception exception)
        {
            using (LogContext.PushProperty("MessageId", messageDto?.TelegramMessageId))
            using (LogContext.PushProperty("ChatId", messageDto?.TelegramChatId))
            using (LogContext.PushProperty("IsResolvingLinkSuccessful", messageDto?.IsSuccessful))
            using (LogContext.PushProperty("YoutubeLinks", messageDto?.YoutubeLink))
            using (LogContext.PushProperty("DownloadLinks", messageDto?.DownloadLinks))
            {
                _logger.Error(exception,
                    "Exception occured while processing exception of!! rabbitmq message, first EX: {@ex}", e);
            }
        }
    }

    private void HandlerMessageProcessingError(Exception e, LinkResolverMessageDto? messageDto, IServiceScope scope, CancellationToken stoppingToken)
    {
        try
        {
            if (messageDto is not null)
            {
                var downloadMetricsService = scope.ServiceProvider.GetRequiredService<DownloadMetricsService>();
                downloadMetricsService.AddAsync(messageDto.TelegramChatId,
                        DownloadMetricsStatus.Failed,
                        0,
                        messageDto.YoutubeLink,
                        messageDto.TelegramMessageId)
                    .GetAwaiter().GetResult();

                var text = "متاسفانه خطایی رخ داد.\n" +
                           $"ای دی : {messageDto.TelegramMessageId}\n" +
                           "لینک ارسالی رو بررسی کن، اگر خطا باز رخ داد با پشتیبانی در تماس باش.";

                _telegramMessageService.SendTextMessage(
                    chatId: messageDto.TelegramChatId,
                    text: text);
            }

            using (LogContext.PushProperty("MessageId", messageDto?.TelegramMessageId))
            using (LogContext.PushProperty("ChatId", messageDto?.TelegramChatId))
            using (LogContext.PushProperty("IsResolvingLinkSuccessful", messageDto?.IsSuccessful))
            using (LogContext.PushProperty("YoutubeLinks", messageDto?.YoutubeLink))
            using (LogContext.PushProperty("DownloadLinks", messageDto?.DownloadLinks))
            {
                _logger.Error(e, "Exception occured while processing rabbitmq message");
            }
        }
        catch (Exception exception)
        {
            using (LogContext.PushProperty("MessageId", messageDto?.TelegramMessageId))
            using (LogContext.PushProperty("ChatId", messageDto?.TelegramChatId))
            using (LogContext.PushProperty("IsResolvingLinkSuccessful", messageDto?.IsSuccessful))
            using (LogContext.PushProperty("YoutubeLinks", messageDto?.YoutubeLink))
            using (LogContext.PushProperty("DownloadLinks", messageDto?.DownloadLinks))
            {
                _logger.Error(exception,
                    "Exception occured while processing exception of!! rabbitmq message, first EX: {@ex}", e);
            }
        }
    }
}