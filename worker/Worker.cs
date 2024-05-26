using Azure.Messaging.ServiceBus;
using N8.Shared.Serializers;
using N8.Worker.Saga;
using System.Text.Json;

namespace N8.Worker;

public class CustomerProvisioningWorker(ServiceBusClient serviceBusClient, SagaOrchestrator sagaOrchestrator,
    ILogger<CustomerProvisioningWorker> logger) : BackgroundService
{
    private ServiceBusReceiver? _receiver;
    private readonly ServiceBusClient _serviceBusClient = serviceBusClient;
    private readonly SagaOrchestrator _sagaOrchestrator = sagaOrchestrator;
    private const string provisioningQueue = "newcustomer";
    private readonly ILogger<CustomerProvisioningWorker> _logger = logger;

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

                    if (sagaMessage == null)
                    {
                        _logger.LogError("Failed to deserialize the message.");
                        await _receiver.AbandonMessageAsync(message, cancellationToken: stoppingToken);
                        continue;
                    }

                    await _sagaOrchestrator.ProcessAsync(sagaMessage, cancellationToken: stoppingToken);

                    await _receiver.CompleteMessageAsync(message, cancellationToken: stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the message.");
            }
        }
    }
}
