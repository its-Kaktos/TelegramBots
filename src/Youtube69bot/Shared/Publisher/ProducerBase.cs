using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Serilog;

namespace Youtube69bot.Shared.Publisher
{
    public interface IRabbitMqProducer<in T>
    {
        void Publish(T @event);
    }

    public abstract class ProducerBase<T> : RabbitMqClientBase, IRabbitMqProducer<T>
    {
        private readonly ILogger _logger;
        private readonly RabbitMqConfig _config;

        protected ProducerBase(
            ConnectionFactory connectionFactory,
            ILogger logger,
            RabbitMqConfig rabbitMqConfig) :
            base(connectionFactory, logger, rabbitMqConfig)
        {
            _logger = logger.ForContext<ProducerBase<T>>() ?? throw new ArgumentNullException(nameof(logger));
            _config = rabbitMqConfig;
        }

        public virtual void Publish(T @event)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event));
                var properties = Channel.CreateBasicProperties();
                properties.AppId = _config.AppId;
                properties.ContentType = "application/json";
                properties.DeliveryMode = 1; // Doesn't persist to disk
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                Channel.BasicPublish(exchange: _config.ExchangeName, routingKey: _config.RoutingKey, body: body, basicProperties: properties);
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Error while publishing");
            }
        }
    }
}