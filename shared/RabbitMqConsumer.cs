using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace N8.Shared.Messaging;

public class ServiceBusConsumer : IAsyncDisposable
{
    private readonly string _queueName;
    private ServiceBusClient _client;
    private ServiceBusProcessor _processor;

    public event Action<string> OnMessageReceived;

    public async Task InitializeAsync(ServiceBusClient client)
    {
        _client = client;

        _processor = _client.CreateProcessor("ha", new ServiceBusProcessorOptions());

        _processor.ProcessMessageAsync += MessageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;

        await _processor.StartProcessingAsync();
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        OnMessageReceived?.Invoke(message);

        await args.CompleteMessageAsync(args.Message);
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        // Handle error here, e.g., log it
        Console.WriteLine(args.Exception.ToString());
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_processor != null)
        {
            await _processor.StopProcessingAsync();
            await _processor.DisposeAsync();
        }

        if (_client != null)
        {
            await _client.DisposeAsync();
        }
    }
}
