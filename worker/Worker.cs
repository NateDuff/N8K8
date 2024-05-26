using Azure;
using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using N8.Shared.Saga;
using N8.Shared.Serializers;
using N8.Worker.Saga;
using Stateless;
using System.Text.Json;

namespace N8.Worker;

//public class Worker : BackgroundService
//{
//    private readonly ILogger<Worker> _logger;
//    //private readonly RabbitMqPublisher _publisher;
//    private readonly ServiceBusPublisher _publisher;

//    public Worker(ILogger<Worker> logger, ServiceBusPublisher publisher)
//    {
//        _logger = logger;
//        _publisher = publisher;
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            try
//            {
//                var message = $"Worker running at: {DateTimeOffset.Now}";

//                await _publisher.PublishMessageAsync(message);

//                if (_logger.IsEnabled(LogLevel.Information))
//                {
//                    _logger.LogInformation(message);
//                }

//                await Task.Delay(3000, stoppingToken);
//            }
//            catch (Exception exception)
//            {
//                _logger.LogError(exception, "An error occurred while publishing a message.");
//            }
//        }
//    }
//}

public class CustomerProvisioningWorker : BackgroundService
{
    private ServiceBusReceiver _receiver;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly SagaOrchestrator _sagaOrchestrator;
    private const string provisioningQueue = "newcustomer";
    private readonly ILogger<CustomerProvisioningWorker> _logger;

    public CustomerProvisioningWorker(ServiceBusClient serviceBusClient, SagaOrchestrator sagaOrchestrator, ILogger<CustomerProvisioningWorker> logger)
    {
        _logger = logger;
        _serviceBusClient = serviceBusClient;
        _sagaOrchestrator = sagaOrchestrator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _receiver = _serviceBusClient.CreateReceiver(provisioningQueue);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _receiver.ReceiveMessageAsync(cancellationToken: stoppingToken);

                if (message != null)
                {
                    var sagaMessageJson = message.Body.ToString();
                    var sagaMessage = JsonSerializer.Deserialize(sagaMessageJson, SagaMessageSerializerContext.Default.SagaMessage);

                    await _sagaOrchestrator.ProcessAsync(sagaMessage);

                    await _receiver.CompleteMessageAsync(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the message.");
            }
        }
    }
}
