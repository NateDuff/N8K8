//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Hosting;
//using System.Collections.Concurrent;

//namespace N8.Shared.Messaging;

//public class MessageQueueService : BackgroundService
//{
//    //private readonly RabbitMqConsumer _consumer;
//    private readonly ServiceBusConsumer _consumer;
//    private readonly ConcurrentQueue<string> _messages = new ConcurrentQueue<string>();

//    public event Action<string> OnMessageReceived;

//    public MessageQueueService(IConfiguration configuration)
//    {
//        //_consumer = new RabbitMqConsumer(connectionFactory);
//        _consumer = new ServiceBusConsumer();
//        _consumer.OnMessageReceived += HandleMessageReceived;
//    }

//    private void HandleMessageReceived(string message)
//    {
//        _messages.Enqueue(message);
//        OnMessageReceived?.Invoke(message);
//    }

//    public void Connect()
//    {
//        if (!_consumer.IsConnected)
//        {
//            _consumer.Initialize();
//        }
//    }

//    protected override Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        return Task.CompletedTask;
//    }

//    public ConcurrentQueue<string> GetMessages() => _messages;

//    public override void Dispose()
//    {
//        _consumer.Dispose();
//        base.Dispose();
//    }
//}
