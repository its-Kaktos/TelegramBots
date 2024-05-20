using RabbitMQ.Client;
using Serilog;

namespace Instagram69Bot.Shared.Publisher
{
    public abstract class RabbitMqClientBase : IDisposable
    {
        protected IModel Channel { get; private set; }
        private IConnection _connection;
        private readonly ConnectionFactory _connectionFactory;
        private readonly ILogger _logger;

        protected RabbitMqClientBase(
            ConnectionFactory connectionFactory,
            ILogger logger,
            RabbitMqConfig rabbitMqConfig)
        {
            _connectionFactory = connectionFactory;
            _logger = logger.ForContext<RabbitMqClientBase>() ?? throw new ArgumentNullException(nameof(logger));
            ConnectToRabbitMq(rabbitMqConfig.ExchangeName, rabbitMqConfig.QueueName, rabbitMqConfig.RoutingKey);
        }

        private void ConnectToRabbitMq(string exchangeName, string queueName, string routingKey)
        {
            if (_connection == null || _connection.IsOpen == false)
            {
                _connection = _connectionFactory.CreateConnection();
            }

            if (Channel == null || Channel.IsOpen == false)
            {
                Channel = _connection.CreateModel();
                // Dont prefetch more than 1 data, see "Fair dispatch": https://www.rabbitmq.com/tutorials/tutorial-two-dotnet.html
                Channel.BasicQos(prefetchSize: 0, prefetchCount: 3, global: false);
                Channel.ExchangeDeclare(exchange: exchangeName, type: "direct", durable: true, autoDelete: false);
                Channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
                Channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: routingKey);
            }
        }

        public void Dispose()
        {
            try
            {
                Channel?.Close();
                Channel?.Dispose();

                _connection?.Close();
                _connection?.Dispose();

                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Cannot dispose RabbitMQ channel or connection");
            }
        }
    }
}