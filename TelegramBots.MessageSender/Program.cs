using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ObjectPool;
using Serilog;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using TelegramBots.MessageSender;
using TelegramBots.MessageSender.Data;
using TelegramBots.MessageSender.DTOs.BotClientOptions;
using TelegramBots.MessageSender.DTOs.BotConfigurations;
using TelegramBots.MessageSender.Pools;
using TelegramBots.MessageSender.Pools.Instagram;
using TelegramBots.MessageSender.Services;
using TelegramBots.MessageSender.Services.BotClients;
using TelegramBots.MessageSender.Services.MessageSenders;
using TelegramBots.MessageSender.Services.Queues;
using TelegramBots.MessageSender.Shared;


// TODO maybe change receiver to Async? 
IHost host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
            .WithDefaultDestructurers()
            .WithDestructurers(new[] { new DbUpdateExceptionDestructurer() })))
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<YoutubeCacheDbContext>(options =>
                options.UseSqlServer(context.Configuration.GetConnectionString("YoutubeCache")),
            ServiceLifetime.Scoped,
            ServiceLifetime.Scoped);

        services.AddDbContext<InstagramCacheDbContext>(options =>
                options.UseSqlServer(context.Configuration.GetConnectionString("InstagramCache")),
            ServiceLifetime.Scoped,
            ServiceLifetime.Scoped);

        services.AddHttpClient();
        services.AddSingleton<InstagramObjectPoolProvider>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var logger = provider.GetRequiredService<ILogger>();
            return new InstagramObjectPoolProvider(httpClientFactory, logger)
            {
#if DEBUG
                MaximumRetained = 3
#else
                MaximumRetained = 50
#endif
            };
        });

        services.AddSingleton<YoutubeObjectPoolProvider>(provider =>
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var logger = provider.GetRequiredService<ILogger>();
            return new YoutubeObjectPoolProvider(httpClientFactory, logger)
            {
#if DEBUG
                MaximumRetained = 3
#else
                MaximumRetained = 50
#endif
            };
        });

        services.AddSingleton<ObjectPool<InstagramTelegramBotClient>>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var telegramOptions = serviceProvider.GetRequiredService<InstagramBotClientOptions>();
            var policy = new InstagramBotClientPooledObjectPolicy(httpClientFactory, telegramOptions);
            var provider = serviceProvider.GetRequiredService<InstagramObjectPoolProvider>();

            return provider.Create(policy);
        });

        services.AddSingleton<ObjectPool<YoutubeTelegramBotClient>>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var telegramOptions = serviceProvider.GetRequiredService<YoutubeBotClientOptions>();
            var policy = new YoutubeBotClientPooledObjectPolicy(httpClientFactory, telegramOptions);
            var provider = serviceProvider.GetRequiredService<YoutubeObjectPoolProvider>();

            return provider.Create(policy);
        });

        services.Configure<InstagramBotConfiguration>(context.Configuration.GetSection(InstagramBotConfiguration.Configuration));
        services.Configure<YoutubeBotConfiguration>(context.Configuration.GetSection(YoutubeBotConfiguration.Configuration));
        services.Configure<RabbitMqServerConfig>(context.Configuration.GetSection("RabbitMqServerConfig"));
        services.Configure<RabbitMqConfig>(context.Configuration.GetSection("RabbitMqConfig"));

        services.AddSingleton<InstagramBotClientOptions>(provider =>
        {
            var botConfig = provider.GetConfiguration<InstagramBotConfiguration>();
            var options = new InstagramBotClientOptions(botConfig.BotToken, botConfig.BaseUrl);
            return options;
        });

        services.AddSingleton<YoutubeBotClientOptions>(provider =>
        {
            var botConfig = provider.GetConfiguration<YoutubeBotConfiguration>();
            var options = new YoutubeBotClientOptions(botConfig.BotToken, botConfig.BaseUrl);
            return options;
        });

        if (context.HostingEnvironment.IsDevelopment())
        {
            services.AddHttpClient("proxy")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    Proxy = new WebProxy
                    {
                        Address = new Uri($"http://127.0.0.1:1389"),
                        BypassProxyOnLocal = false,
                        UseDefaultCredentials = false
                    }
                });
        }

        services.AddSingleton<MessageHandlerService>();

        services.AddSingleton<InstagramOperationsQueueService>();
        services.AddSingleton<YoutubeOperationsQueueService>();

        services.AddHostedService<InstagramSendMessagesHostedService>();
        services.AddHostedService<YoutubeSendMessagesHostedService>();

        services.AddHostedService<MainHostedService>();

        services.AddMemoryCache();

        services.AddSingleton<FileCacheService>();
        services.AddTransient<MigrateDatabaseService>();
    }).Build();

var logger = host.Services.GetRequiredService<ILogger>();
logger.Debug("Starting application");

Log.Logger = logger;

try
{
    using (var scope = host.Services.CreateScope())
    {
        var seedDb = scope.ServiceProvider.GetRequiredService<MigrateDatabaseService>();
        await seedDb.MigrateDatabaseAsync();
    }

    await host.RunAsync();
    logger.Debug("Stopping application");
}
catch (Exception e)
{
    logger.Fatal(e, "Host terminated unexpectedly");
    throw;
}
finally
{
    logger.Information("Closing Message sender application.");
    await Log.CloseAndFlushAsync();
}