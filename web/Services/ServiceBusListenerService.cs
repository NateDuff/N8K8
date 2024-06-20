using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.SignalR;
using N8.Web.Hubs.Hubs;

namespace N8.Web.Services;

public class ServiceBusListenerService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IHubContext<ChatHub> _hubContext;
    private ServiceBusProcessor _processor;

    public ServiceBusListenerService(ServiceBusClient serviceBusClient, IHubContext<ChatHub> hubContext)
    {
        _serviceBusClient = serviceBusClient;
        _hubContext = hubContext;
    }

    public async Task StartAsync(string queueName)
    {
        _processor = _serviceBusClient.CreateProcessor(queueName, new ServiceBusProcessorOptions());

        _processor.ProcessMessageAsync += MessageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;

        await _processor.StartProcessingAsync();
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        var messageBody = args.Message.Body.ToString();

        await _hubContext.Clients.All.SendAsync("ReceiveMessage", "ADMIN", messageBody, DateTime.UtcNow);

        await args.CompleteMessageAsync(args.Message);
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        Console.WriteLine($"Message handler encountered an exception {args.Exception}.");
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        await _processor.StopProcessingAsync();
        await _processor.DisposeAsync();
    }
}
