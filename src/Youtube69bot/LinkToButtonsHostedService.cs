using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Serilog.Context;
using Youtube69bot.Dapper;
using Youtube69bot.Data;
using Youtube69bot.Services;
using Youtube69bot.Shared;
using Youtube69bot.Shared.MessageSender;

namespace Youtube69bot;

public class LinkToButtonsHostedService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly TelegramBotRabbitMqConfig _rabbitMqConfig;
    private readonly RabbitMqServerConfig _rabbitMqServerConfig;
    private readonly TelegramMessageService _telegramMessageService;
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly string BaseFilePath = Path.Combine(Path.GetTempPath(), "saved-thumbnails/");
    private readonly IServiceProvider _serviceProvider;
    private readonly DownloadMetricsService _downloadMetricsService;

    public LinkToButtonsHostedService(ILogger logger, IServiceProvider serviceProvider, TelegramMessageService telegramMessageService, IHttpClientFactory httpClientFactory, DownloadMetricsService downloadMetricsService)
    {
        Directory.CreateDirectory(BaseFilePath);

        _serviceProvider = serviceProvider;
        _telegramMessageService = telegramMessageService;
        _httpClientFactory = httpClientFactory;
        _downloadMetricsService = downloadMetricsService;
        _logger = logger.ForContext<LinkToButtonsHostedService>();
        _rabbitMqConfig = serviceProvider.GetConfiguration<TelegramBotRabbitMqConfig>();
        _rabbitMqServerConfig = serviceProvider.GetConfiguration<RabbitMqServerConfig>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Debug(nameof(LinkToButtonsHostedService) + " host start");

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
                    _logger.Debug("Received message");
                    MessageReceivedEventHandler(model, ea);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Exception occured while processing rabbitmq message");
                    channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, false);
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
                await Task.Delay(500, stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
        catch (Exception e)
        {
            _logger.Fatal(e, "Host terminated unexpectedly");
            throw;
        }
        finally
        {
            _logger.Information("Host stopped");
            await Log.CloseAndFlushAsync();
        }
    }

    private void MessageReceivedEventHandler(object sender, BasicDeliverEventArgs ea)
    {
        var messageJson = Encoding.UTF8.GetString(ea.Body.ToArray());
        _logger.Debug("Message received, message: {message}", messageJson);

        var messageDto = JsonSerializer.Deserialize<LinkToButtonMessageDto>(messageJson);
        if (messageDto is null) throw new InvalidOperationException("Send message from rabbitmq is null.");
        _logger.Information("Message received, message: {@Message}", messageDto);

        if (!messageDto.IsSuccessful || messageDto.IsMediaNotFound || messageDto.Exception is not null)
        {
            HandleErrorAsync(messageDto).GetAwaiter().GetResult();
            return;
        }

        var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        HandleMessageAsync(dbContext, messageDto).GetAwaiter().GetResult();
    }

    private async Task HandleErrorAsync(LinkToButtonMessageDto messageDto)
    {
        _telegramMessageService.DeleteMessage(messageDto.TelegramChatId, messageDto.ReplyMessageId!);

        await _downloadMetricsService.AddAsync(messageDto.TelegramChatId,
            DownloadMetricsStatus.Failed,
            0,
            messageDto.YoutubeLink,
            messageDto.TelegramMessageId);

        if (messageDto.IsMediaNotFound || messageDto.YoutubeErrorMessage == "NOTFOUND")
        {
            _telegramMessageService.SendTextMessage(messageDto.TelegramChatId,
                "🚨 لینک ارسالی پیدا نشد، بررسی کن که لینک معتبر باشه \n" +
                $"ای دی: {messageDto.TelegramMessageId} \n" +
                "اگر حس میکنی مشکلی پیش اومده با پشتیبانی در تماس باش");

            _logger
                .ForContext("YoutubeLink", messageDto.YoutubeLink)
                .Warning("Media not found");
            return;
        }

        switch (messageDto.YoutubeErrorMessage)
        {
            case "LIVE":
                _telegramMessageService.SendTextMessage(messageDto.TelegramChatId,
                    "⚠️ دوست من، به دلیل محدودیت یوتیوب، امکان دانلود ویدیو لایو (زنده) وجود ندارد \n" +
                    $"ای دی: {messageDto.TelegramMessageId} \n" +
                    "اگر حس میکنی مشکلی پیش اومده با پشتیبانی در تماس باش");

                _logger
                    .ForContext("YoutubeLink", messageDto.YoutubeLink)
                    .Warning("Media is live");
                return;
            case "SINGIN":
            case "PRIVATE":
                _telegramMessageService.SendTextMessage(messageDto.TelegramChatId,
                    "\u26a0\ufe0f دوست من، لینک ارسالیت به دلیل محدودیت یوتیوب قابل دانلود شدن نیست :( \n" +
                    $"ای دی: {messageDto.TelegramMessageId} \n" +
                    "اگر حس میکنی مشکلی پیش اومده با پشتیبانی در تماس باش");

                _logger
                    .ForContext("YoutubeLink", messageDto.YoutubeLink)
                    .Warning("Media is private and/or sing-in is needed");
                return;
            case "LINKERROR":
                _telegramMessageService.SendTextMessage(messageDto.TelegramChatId,
                    "\ud83e\udd37\u200d\u2642\ufe0f دوست من، لینک ارسالی پیدا نشد \n" +
                    $"ای دی: {messageDto.TelegramMessageId} \n" +
                    "اگر حس میکنی مشکلی پیش اومده با پشتیبانی در تماس باش");

                _logger
                    .ForContext("YoutubeLink", messageDto.YoutubeLink)
                    .Warning("Provided link is not valid");

                return;
        }

        if (messageDto.Exception is not null)
        {
            _telegramMessageService.SendTextMessage(messageDto.TelegramChatId,
                " \ud83d\ude45\u200d\u2642\ufe0fمتاسفانه سرور یوتیوب در دسترس نیست، لطفا دوباره تلاش کنید. \n" +
                $"ای دی: {messageDto.TelegramMessageId} \n" +
                "اگر حس میکنی مشکلی پیش اومده با پشتیبانی در تماس باش");

            _logger.Debug("No exception occured and no media link has been found");
            return;
        }

        using (LogContext.PushProperty("Exception", messageDto.Exception))
        using (LogContext.PushProperty("ExceptionType", messageDto.ExceptionType))
        {
            var text = "متاسفانه خطایی رخ داد.\n" +
                       $"ای دی : {messageDto.TelegramMessageId}\n" +
                       "لینک ارسالی رو بررسی کن، اگر خطا باز رخ داد با پشتیبانی در تماس باش.";

            _telegramMessageService.SendTextMessage(
                chatId: messageDto.TelegramChatId,
                text: text);

            _logger.Error("Error occured in link resolver, exception data: {Exception}", messageDto.ExceptionData);
        }
    }

    private async Task HandleMessageAsync(ApplicationDbContext dbContext, LinkToButtonMessageDto messageDto)
    {
        var resolvedYoutubeLinks = new List<ResolvedYoutubeLink>();
        var keyboardButtons = new List<IEnumerable<TelegramKeyboardButton>>();

        var videoButtons = GetVideoButtons(messageDto, resolvedYoutubeLinks);
        if (videoButtons.Count > 0)
        {
            keyboardButtons.Add(new[]
            {
                new TelegramKeyboardButton
                {
                    Text = "دانلود ویدیو \u2b07\ufe0f",
                    CallbackData = "EMPTY"
                }
            });
            keyboardButtons.Add(videoButtons);
        }

        var audioButtons = GetAudioButtons(messageDto, resolvedYoutubeLinks);
        if (audioButtons.Count > 0)
        {
            keyboardButtons.Add(new[]
            {
                new TelegramKeyboardButton
                {
                    Text = "دانلود فقط صدای ویدیو \u2b07\ufe0f",
                    CallbackData = "EMPTY"
                }
            });

            keyboardButtons.Add(audioButtons);
        }

        var viewsCount = messageDto.ViewsCount is null ? "" : messageDto.ViewsCount.Value.ToString("#,###");
        var duration = messageDto.DurationInSeconds is null
            ? ""
            : TimeSpan.FromSeconds(messageDto.DurationInSeconds.Value).ToString("hh':'mm':'ss");

        var description = $"""
                            نام : {messageDto.Title}
                            بازدید : {viewsCount}
                            زمان : {duration}
                            
                            ⭐ دوست من، برای دانلود فقط صدای ویدیو، کیفیت صدا رو از پایین ترین دکمه ها انتخاب کن!
                            کیفیت مورد نظرت رو انتخاب کن 👇
                           """;
        _telegramMessageService.DeleteMessage(messageDto.TelegramChatId, messageDto.ReplyMessageId);

#if DEBUG
        var replyMessageId = _telegramMessageService.SendTextMessage(messageDto.TelegramChatId,
            description,
            keyboardButtons);
#else
        var filePath = await DownloadThumbnailAsync(messageDto.ThumbnailLink!);
        var replyMessageId = _telegramMessageService.SendPhotoMessage(messageDto.TelegramChatId,
            filePath,
            description,
            keyboardButtons);
#endif

        await AddLinksToDbAsync(dbContext, resolvedYoutubeLinks, replyMessageId);
    }

    private static List<TelegramKeyboardButton> GetVideoButtons(LinkToButtonMessageDto messageDto, List<ResolvedYoutubeLink> resolvedYoutubeLinks)
    {
        var videoButtons = new List<TelegramKeyboardButton>(2);

        var videos = messageDto.VideosWithSound.Where(x => x.Height >= 360).ToList();
        if (videos.Count < 2)
        {
            videos.Clear();

            var videosInOrder = messageDto.VideosWithSound
                .OrderByDescending(x => x.Height)
                .ToList();

            var lowQualityIndex = (videosInOrder.Count - 1) / 2;

            var highestQualityVideo = videosInOrder[0];
            var lowQualityVideo = videosInOrder[lowQualityIndex];
            if (highestQualityVideo != lowQualityVideo)
            {
                videos.Add(lowQualityVideo);
            }

            videos.Add(highestQualityVideo);
        }

        foreach (var video in videos)
        {
            var text = $"🎥 {video.Quality}";

            var callbackData = "ytrl_" + Guid.NewGuid();
            videoButtons.Add(new TelegramKeyboardButton
            {
                Text = text,
                CallbackData = callbackData
            });

            resolvedYoutubeLinks.Add(new ResolvedYoutubeLink
            {
                Id = callbackData,
                YoutubeLink = messageDto.YoutubeLink,
                ReplyMessageId = messageDto.ReplyMessageId,
                TelegramChatId = messageDto.TelegramChatId,
                TelegramMessageId = messageDto.TelegramMessageId,
                AddedDate = DateTimeOffset.Now,
                DownloadLink = video.DownloadLink,
                Title = messageDto.Title,
                ThumbnailLink = messageDto.ThumbnailLink,
                VideoHeight = video.Height,
                VideoWidth = video.Width,
                DurationInSeconds = messageDto.DurationInSeconds
            });
        }

        return videoButtons;
    }

    private static List<TelegramKeyboardButton> GetAudioButtons(LinkToButtonMessageDto messageDto, List<ResolvedYoutubeLink> resolvedYoutubeLinks)
    {
        var audioButtons = new List<TelegramKeyboardButton>(2);

        var audioBestQuality = messageDto.Audios.OrderByDescending(a => a.Quality).First();
        var audioWorstQuality = messageDto.Audios.OrderByDescending(a => a.Quality).Last();
        if (audioBestQuality != audioWorstQuality)
        {
            var audioWorstQualityCallbackData = "ytrl_" + Guid.NewGuid();
            audioButtons.Add(new TelegramKeyboardButton
            {
                Text = "کیفیت پایین 🎵",
                CallbackData = audioWorstQualityCallbackData
            });

            resolvedYoutubeLinks.Add(new ResolvedYoutubeLink
            {
                Id = audioWorstQualityCallbackData,
                YoutubeLink = messageDto.YoutubeLink,
                ReplyMessageId = "",
                TelegramChatId = messageDto.TelegramChatId,
                TelegramMessageId = messageDto.TelegramMessageId,
                AddedDate = DateTimeOffset.Now,
                DownloadLink = audioWorstQuality.DownloadLink,
                Title = messageDto.Title,
                ThumbnailLink = messageDto.ThumbnailLink,
                AudioQuality = audioWorstQuality.Quality
            });
        }

        var audioBestQualityCallbackData = "ytrl_" + Guid.NewGuid();
        audioButtons.Add(new TelegramKeyboardButton
        {
            Text = "کیفیت عالی 🎵",
            CallbackData = audioBestQualityCallbackData
        });

        resolvedYoutubeLinks.Add(new ResolvedYoutubeLink
        {
            Id = audioBestQualityCallbackData,
            YoutubeLink = messageDto.YoutubeLink,
            ReplyMessageId = messageDto.ReplyMessageId,
            TelegramChatId = messageDto.TelegramChatId,
            TelegramMessageId = messageDto.TelegramMessageId,
            AddedDate = DateTimeOffset.Now,
            DownloadLink = audioBestQuality.DownloadLink,
            Title = messageDto.Title,
            ThumbnailLink = messageDto.ThumbnailLink,
            AudioQuality = audioBestQuality.Quality
        });

        return audioButtons;
    }

    private static async Task AddLinksToDbAsync(ApplicationDbContext dbContext, List<ResolvedYoutubeLink> youtubeLinks, string replyMessageId)
    {
        foreach (var link in youtubeLinks)
        {
            link.ReplyMessageId = replyMessageId;
        }

        await dbContext.ResolvedYoutubeLinks.AddRangeAsync(youtubeLinks);
        await dbContext.SaveChangesAsync();
    }

    private async Task<string> DownloadThumbnailAsync(string thumbnailUri, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient("proxy");
        using var response = await httpClient.GetAsync(thumbnailUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var contentType = response.Content.Headers.ContentType;

        var fileExtension = GetFileExtensionFromContentType(contentType!.MediaType!);
        var filePath = Path.Combine(BaseFilePath, $"{Guid.NewGuid()}{fileExtension}");
        await using var destinationStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var download = await response.Content.ReadAsStreamAsync(cancellationToken);
        await download.CopyToAsync(destinationStream, cancellationToken);


        return filePath;
    }

    private static string GetFileExtensionFromContentType(string contentType)
    {
        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "video/mp4" => ".mp4",
            "video/quicktime" => ".mov",
            _ => string.Empty
        };
    }

    private ConnectionFactory CreateFactory()
    {
        return new ConnectionFactory
        {
            HostName = _rabbitMqServerConfig.HostName,
            UserName = _rabbitMqServerConfig.UserName,
            Password = _rabbitMqServerConfig.Password,
            Port = _rabbitMqServerConfig.Port, // set to -1 to use default ports
            ConsumerDispatchConcurrency = _rabbitMqServerConfig.ConsumerDispatchConcurrency // CHECK THIS
        };
    }

    private void ConfigureChannel(IModel channel)
    {
        // Dont prefetch more than 1 data, see "Fair dispatch": https://www.rabbitmq.com/tutorials/tutorial-two-dotnet.html
        channel.BasicQos(prefetchSize: 0, prefetchCount: _rabbitMqServerConfig.PrefetchCount, global: false);

        channel.ExchangeDeclare(_rabbitMqConfig.ExchangeName, ExchangeType.Direct, true);

        channel.QueueDeclare(queue: _rabbitMqConfig.QueueName, durable: true, exclusive: false, autoDelete: false);

        channel.QueueBind(_rabbitMqConfig.QueueName, _rabbitMqConfig.ExchangeName, _rabbitMqConfig.RoutingKey);
    }

    private void ShutdownEventHandler(object? sender, ShutdownEventArgs args)
    {
        if (args.ReplyText == "Goodbye") return;

        _logger
            .ForContext("Cause", args.Cause)
            .ForContext("Initiator", args.Initiator)
            .ForContext("ReplyText", args.ReplyText)
            .Fatal("consumer closed??");
    }
}