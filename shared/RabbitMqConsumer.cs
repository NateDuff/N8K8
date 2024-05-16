using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace N8.Shared.Messaging;

public class RabbitMqConsumer
{
    private readonly IConnectionFactory _connectionFactory;
    private IConnection _connection;
    private IModel _channel;

    public event Action<string> OnMessageReceived;

    public RabbitMqConsumer(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        Initialize();
    }

    private void Initialize()
    {
        _connection = _connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(queue: "myqueue", durable: false, exclusive: false, autoDelete: false, arguments: null);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            OnMessageReceived?.Invoke(message);
        };

        _channel.BasicConsume(queue: "myqueue", autoAck: true, consumer: consumer);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
