using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using TelegramBots.MessageSender.DTOs;
using TelegramBots.MessageSender.Services;
using TelegramBots.MessageSender.Shared;

namespace TelegramBots.MessageSender;

public class MainHostedService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly RabbitMqConfig _rabbitMqConfig;
    private readonly RabbitMqServerConfig _rabbitMqServerConfig;
    private readonly MessageHandlerService _messageHandlerService;

    public MainHostedService(ILogger logger, IServiceProvider serviceProvider, MessageHandlerService messageHandlerService)
    {
        _logger = logger.ForContext<MainHostedService>();
        _messageHandlerService = messageHandlerService;
        _rabbitMqConfig = serviceProvider.GetConfiguration<RabbitMqConfig>();
        _rabbitMqServerConfig = serviceProvider.GetConfiguration<RabbitMqServerConfig>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Debug("Main message host start");

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

        var messageDto = JsonSerializer.Deserialize<MessageDto>(messageJson);
        if (messageDto is null) throw new InvalidOperationException("Send message from rabbitmq is null.");
        _logger.Information("Message received, message: {@Message}", messageJson);

        _messageHandlerService.Handle(messageDto);
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