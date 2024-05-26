using Azure.Messaging.ServiceBus;
using System.Text;

namespace N8.Shared.Messaging;

public class ServiceBusPublisher : IAsyncDisposable
{
    private readonly string _queueName = "ha";
    private ServiceBusClient _client;
    private ServiceBusSender _sender;

    public ServiceBusPublisher(ServiceBusClient client)
    {
        _client = client;
        _sender = _client.CreateSender(_queueName);
    }

    public async Task PublishMessageAsync(string message)
    {
        if (_sender == null)
        {
            throw new InvalidOperationException("ServiceBusSender is not initialized.");
        }

        var body = Encoding.UTF8.GetBytes(message);
        var serviceBusMessage = new ServiceBusMessage(body);
        await _sender.SendMessageAsync(serviceBusMessage);
    }

    public async ValueTask DisposeAsync()
    {
        if (_sender != null)
        {
            await _sender.DisposeAsync();
        }

        if (_client != null)
        {
            await _client.DisposeAsync();
        }
    }
}
