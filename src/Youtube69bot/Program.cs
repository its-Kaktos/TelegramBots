using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using RabbitMQ.Client;
using Serilog;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using Telegram.Bot;
using Youtube69bot;
using Youtube69bot.Commands;
using Youtube69bot.Dapper;
using Youtube69bot.Data;
using Youtube69bot.Publisher;
using Youtube69bot.Quartz;
using Youtube69bot.Quartz.JobFactories;
using Youtube69bot.Quartz.Jobs;
using Youtube69bot.Services;
using Youtube69bot.Shared;
using Youtube69bot.Shared.MessageSender;
using Youtube69bot.Shared.Publisher;
using QuartzHostedService = Youtube69bot.Quartz.QuartzHostedService;

// TODO FIx 09 Jan 2024 12:57:40.103
// TODO Fix 3860, when this exception happens resend the rabbitmq message to be processed again?
// TODO Maybe dl with YT_DLP to avoid dl speed throtling? Nope
// TODO Download 1080p?
// TODO Every user can download 1 link per minute?
// TODO instagram dl captions as well
// TODO ANIME BOT!!!!
IHost host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
            .WithDefaultDestructurers()
            .WithDestructurers(new[] { new DbUpdateExceptionDestructurer() })))
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(context.Configuration.GetConnectionString("mssql")),
            ServiceLifetime.Transient,
            ServiceLifetime.Transient);

        services.Configure<BotConfiguration>(context.Configuration.GetSection(BotConfiguration.Configuration));
        services.Configure<RabbitMqServerConfig>(context.Configuration.GetSection("RabbitMqServerConfig"));

        services.Configure<TelegramMessageSenderRabbitMqConfig>(context.Configuration.GetSection("RabbitMqConfig_TelegramMessageEvent"));
        services.Configure<YoutubeLinkResloverRabbitMqConfig>(context.Configuration.GetSection("RabbitMqConfig_YoutubeLinkResolverEvent"));
        services.Configure<YoutubeDownloaderRabbitMqConfig>(context.Configuration.GetSection("RabbitMqConfig_YoutubeDownloaderEvent"));
        services.Configure<TelegramBotRabbitMqConfig>(context.Configuration.GetSection("RabbitMqConfig_TelegramBotEvent"));


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
        services.AddScoped<AdminCommand>();
        services.AddScoped<DownloadFromYoutubeCommand>();
        services.AddScoped<BulkMessageSender>();
        services.AddScoped<UserService>();

        services
            .AddSingleton<IRabbitMqProducer<YoutubeLinkResolveEvent>, DownloadYoutubePublisher>(provider =>
            {
                var connectionFactory = provider.GetRequiredService<ConnectionFactory>();
                var logger = provider.GetRequiredService<ILogger>();
                var rabbitMqConfig = provider.GetConfiguration<YoutubeLinkResloverRabbitMqConfig>();

                return new DownloadYoutubePublisher(connectionFactory, logger, rabbitMqConfig);
            })
            .AddSingleton<IRabbitMqProducer<TelegramMessageEvent>, TelegramMessagePublisher>(provider =>
            {
                var connectionFactory = provider.GetRequiredService<ConnectionFactory>();
                var logger = provider.GetRequiredService<ILogger>();
                var rabbitMqConfig = provider.GetConfiguration<TelegramMessageSenderRabbitMqConfig>();

                return new TelegramMessagePublisher(connectionFactory, logger, rabbitMqConfig);
            })
            .AddSingleton<IRabbitMqProducer<YoutubeDownloadEvent>, DownloaderMessagePublisher>(provider =>
            {
                var connectionFactory = provider.GetRequiredService<ConnectionFactory>();
                var logger = provider.GetRequiredService<ILogger>();
                var rabbitMqConfig = provider.GetConfiguration<YoutubeDownloaderRabbitMqConfig>();

                return new DownloaderMessagePublisher(connectionFactory, logger, rabbitMqConfig);
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

        services.AddSingleton<DownloadMetricsService>(_ => new DownloadMetricsService(context.Configuration.GetConnectionString("Youtube69BotMetrics")!));

        services.AddSingleton<TelegramMessageService>();
        services.AddSingleton<DownloadLinkCallbackHandler>();

        services.AddMemoryCache();

        services.AddHostedService<LinkToButtonsHostedService>();

        // Add Quartz services
        services.AddSingleton<IJobFactory, SingletonJobFactory>();
        services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();

        // Add the jobs
        services.AddSingleton<RemoveExpiredLinksFromDbJob>();
        services.AddSingleton(new JobSchedule(
            jobType: typeof(RemoveExpiredLinksFromDbJob),
            cronExpression: "1 0 00 * * ?")); // run on every 00:00:01 hour ( 12:00:01AM )

        services.AddHostedService<QuartzHostedService>();
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

namespace Youtube69bot
{
    public class BotConfiguration
    {
        public static readonly string Configuration = "BotConfiguration";

        public string BotToken { get; set; } = "";
        public string? BaseUrl { get; set; }
    }
}