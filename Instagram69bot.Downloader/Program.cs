using System.Net;
using Instagram69bot.Downloader;
using Instagram69bot.Downloader.Dapper;
using Instagram69bot.Downloader.DTOs;
using Instagram69bot.Downloader.Services;
using Instagram69bot.Downloader.Shared;
using Instagram69bot.Downloader.Shared.Publisher;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Serilog;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using Telegram.Bot;

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
        services.Configure<BotConfiguration>(context.Configuration.GetSection(BotConfiguration.Configuration));
        services.Configure<RabbitMqServerConfig>(context.Configuration.GetSection("RabbitMqServerConfig"));
        services.Configure<MTProtoConfiguration>(context.Configuration.GetSection(MTProtoConfiguration.Configuration));

        services.Configure<LinkResolverRabbitMqConfig>(context.Configuration.GetSection("RabbitMqConfig_LinkResolverEvent"));
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
                    var botConfig = sp.GetConfiguration<BotConfiguration>();
                    TelegramBotClientOptions options = new(botConfig.BotToken, botConfig.BaseUrl);

                    return new TelegramBotClient(options, httpClient);
                });

            services.AddHttpClient("proxy");
        }

        services.AddTransient<InstagramDownloader>();
        services.AddTransient<LinkResolverErrorHandler>();

        services.AddHostedService<MainHostedService>();

        services.AddSingleton<FFMpegWrapper>();
        services.AddSingleton<TelegramUploader>();

        services.AddScoped<DownloadMetricsService>(_ => new DownloadMetricsService(context.Configuration.GetConnectionString("Instagram69BotMetrics")!));

        services.AddSingleton<IRabbitMqProducer<TelegramMessageEvent>, TelegramMessagePublisher>(provider =>
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

        services.AddSingleton<TelegramMessageService>();
    }).Build();

var logger = host.Services.GetRequiredService<ILogger>();
logger.Debug("Starting application");

Log.Logger = logger;

try
{
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
    logger.Information("Closing downloader application.");
    await Log.CloseAndFlushAsync();
}

namespace Instagram69bot.Downloader
{
    public class BotConfiguration
    {
        public static readonly string Configuration = "BotConfiguration";

        public string BotToken { get; set; } = "";
        public string? BaseUrl { get; set; }
    }

    public class MTProtoConfiguration
    {
        public static readonly string Configuration = "MTProto";

        public string api_id { get; set; } = "";
        public string api_hash { get; set; } = "";
        public string phone_number { get; set; } = "";
    }
}