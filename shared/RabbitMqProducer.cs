using RabbitMQ.Client;
using System.Text;

namespace N8.Shared.Messaging;

public class RabbitMqPublisher : IDisposable
{
    public bool IsConnected => _channel != null && _channel.IsOpen;

    private readonly IConnectionFactory _connectionFactory;
    private IConnection _connection;
    private IModel _channel;

    public RabbitMqPublisher(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void Initialize()
    {
        _connection = _connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(queue: "myqueue", durable: false, exclusive: false, autoDelete: false, arguments: null);
    }

    public void PublishMessage(string message)
    {
        if (_channel.IsClosed)
        {
            return;
        }

        var body = Encoding.UTF8.GetBytes(message);
        _channel.BasicPublish(exchange: "", routingKey: "myqueue", basicProperties: null, body: body);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
