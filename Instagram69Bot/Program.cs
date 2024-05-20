using System.Net;
using Instagram69Bot;
using Instagram69Bot.Commands;
using Instagram69Bot.Commands.Instagram;
using Instagram69Bot.Dapper;
using Instagram69Bot.Data;
using Instagram69Bot.Publisher;
using Instagram69Bot.Services;
using Instagram69Bot.Shared;
using Instagram69Bot.Shared.MessageSender;
using Instagram69Bot.Shared.Publisher;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Serilog;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using Telegram.Bot;

// TODO CHeck chatid 445759465 to find out why some message guid ids are not found

// TODO Videos, Thumbnails and pictures downloaded are not removed after upload. create a program or job to remove them every 24h?s
// TODO add another website to get download links
// TODO Cache the links in DB, add ability to download again and not use cache?
// TODO maybe limit each user to 2.5gb download each hour after tracking them for sometimes?
// TODO Add more proxy lists
// TODO Send captions too
// TODO Lock database record when updating? to prevent simulations updates?
// TODO Show usage of top 50 people and their daily usage 
// TODO Fix Callbackquery algorithm to only save joined channels for user where user was not aleary joined. right now it adds all channels even if use is already in one of them.s
// TODO Create inlinequery for instagram69bot?
// TODO Make a checker to send links to robots and check their health
// TODO read messages TODO from other projects
// TODO Remove unused nuget packages
// TODO search rabbitmq security in production. read : https://www.rabbitmq.com/production-checklist.html
// TODO WHAT if user is not joined in mandatory channels and the links change? or new version of mandatory channels is available

IHost host = Host.CreateDefaultBuilder(args)
    // .ConfigureLogging(options => options.ClearProviders())
    .UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
            .WithDefaultDestructurers()
            .WithDestructurers(new[] { new DbUpdateExceptionDestructurer() })))
    .ConfigureServices((context, services) =>
    {
        // CorrelationId -> SERILOG PUSH PROPERTY
        services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(context.Configuration.GetConnectionString("mssql")),
            ServiceLifetime.Transient,
            ServiceLifetime.Transient);

        // Register Bot configuration

        services.Configure<BotConfiguration>(context.Configuration.GetSection(BotConfiguration.Configuration));
        services.Configure<RabbitMqServerConfig>(context.Configuration.GetSection("RabbitMqServerConfig"));

        services.Configure<DownloadInstagramRabbitMqConfig>(context.Configuration.GetSection("RabbitMqConfig_DownloadInstagramEvent"));
        services.Configure<TelegramRabbitMqConfig>(context.Configuration.GetSection("RabbitMqConfig_TelegramMessageEvent"));


        if (context.HostingEnvironment.IsDevelopment())
        {
            services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(1_000);
                    var botConfig = sp.GetConfiguration<BotConfiguration>();
                    TelegramBotClientOptions options = new(botConfig.BotToken, botConfig.BaseUrl);

                    return new TelegramBotClient(options, httpClient);
                }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    Proxy = new WebProxy
                    {
                        Address = new Uri($"http://127.0.0.1:1389"),
                        BypassProxyOnLocal = false,
                        UseDefaultCredentials = false
                    }
                });

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
        else
        {
            services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(100);
                    var botConfig = sp.GetConfiguration<BotConfiguration>();
                    TelegramBotClientOptions options = new(botConfig.BotToken, botConfig.BaseUrl);

                    return new TelegramBotClient(options, httpClient);
                });

            services.AddHttpClient("proxy");
        }

        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();

        services.AddScoped<MainCommand>();
        services.AddScoped<DownloadFromInstaCommand>();
        services.AddScoped<AdminCommand>();
        services.AddScoped<BulkMessageSender>();
        services.AddScoped<UserService>();

        services
            .AddSingleton<IRabbitMqProducer<DownloadInstagramEvent>, DownloadInstagramPublisher>(provider =>
            {
                var connectionFactory = provider.GetRequiredService<ConnectionFactory>();
                var logger = provider.GetRequiredService<ILogger>();
                var rabbitMqConfig = provider.GetConfiguration<DownloadInstagramRabbitMqConfig>();

                return new DownloadInstagramPublisher(connectionFactory, logger, rabbitMqConfig);
            })
            .AddSingleton<IRabbitMqProducer<TelegramMessageEvent>, TelegramMessagePublisher>(provider =>
            {
                var connectionFactory = provider.GetRequiredService<ConnectionFactory>();
                var logger = provider.GetRequiredService<ILogger>();
                var rabbitMqConfig = provider.GetConfiguration<TelegramRabbitMqConfig>();

                return new TelegramMessagePublisher(connectionFactory, logger, rabbitMqConfig);
            })
            .AddSingleton(provider =>
            {
                var config = provider.GetConfiguration<RabbitMqServerConfig>();

                return new ConnectionFactory
                {
                    HostName = config.HostName,
                    UserName = config.UserName,
                    Password = config.Password,
                    Port = config.Port // set to -1 to use default ports
                };
            });

        services.AddTransient<SeedDatabaseService>();

        services.AddSingleton<DownloadMetricsService>(_ => new DownloadMetricsService(context.Configuration.GetConnectionString("Instagram69BotMetrics")!));

        services.AddSingleton<TelegramMessageService>();

        services.AddMemoryCache();
    }).Build();

var logger = host.Services.GetRequiredService<ILogger>();
logger.Debug("Starting application");

try
{
    using (var scope = host.Services.CreateScope())
    {
        var seedDb = scope.ServiceProvider.GetRequiredService<SeedDatabaseService>();
        var metricsService = scope.ServiceProvider.GetRequiredService<DownloadMetricsService>();
        await seedDb.SeedDatabaseAsync();
        await metricsService.CreateTableIfDoesNotExistsAsync();
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
    await Log.CloseAndFlushAsync();
}

namespace Instagram69Bot
{
    public class BotConfiguration
    {
        public static readonly string Configuration = "BotConfiguration";

        public string BotToken { get; set; } = "";
        public string? BaseUrl { get; set; }
    }
}