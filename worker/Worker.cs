using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using Azure;
using N8.Shared.Messaging;
using N8.Shared.Saga;
using System.Text.Json;
using N8.Shared.Serializers;

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
    private ServiceBusProcessor _processor;
    private ServiceBusSender _serviceBusSender;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly TableServiceClient _tableServiceClient;
    private readonly TableClient _orchestrationClient;
    private readonly ILogger<CustomerProvisioningWorker> _logger;
    private readonly string provisioningQueue = "newcustomer";

    public CustomerProvisioningWorker(ServiceBusClient serviceBusClient, TableServiceClient tableServiceClient, ILogger<CustomerProvisioningWorker> logger)
    {
        _serviceBusClient = serviceBusClient;
        _tableServiceClient = tableServiceClient;
        _orchestrationClient = _tableServiceClient.GetTableClient("Orchestrations");
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _serviceBusSender = _serviceBusClient.CreateSender(provisioningQueue);
        _processor = _serviceBusClient.CreateProcessor(provisioningQueue, new ServiceBusProcessorOptions());

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var rawMessage = args.Message.Body.ToString();

        var sagaMessage = JsonSerializer.Deserialize(rawMessage, SagaMessageSerializerContext.Default.SagaMessage);

        // Retrieve the orchestration from the table

        var orchestration = await _orchestrationClient.GetEntityAsync<TableEntity>("Orchestration", sagaMessage.SagaId);

        if (sagaMessage.Status != (string)orchestration.Value["Status"])
        {
            _logger.LogWarning($"Status mismatch for saga ID {sagaMessage.SagaId}. Expected: {orchestration.Value["Status"]}, Actual: {sagaMessage.Status}");
        }

        switch (sagaMessage.Status)
        {
            case "New":
                await ProcessCreateCustomerAsync(orchestration);
                break;

            case "CustomerCreated":
                await ProcessCreateSubscriptionAsync(orchestration);
                break;

            case "SubscriptionCreated":
                await ProcessStartPipelineAndSendEmailAsync(orchestration);
                break;

            case "PipelineStartedAndEmailSent":
                await ProcessCloseRequestAsync(orchestration);
                break;

            default:
                _logger.LogWarning($"Unknown status: {orchestration.Value["Status"]}");
                break;
        }

        await args.CompleteMessageAsync(args.Message);
    }

    private async Task ProcessCreateCustomerAsync(TableEntity orchestration)
    {
        var customerClient = _tableServiceClient.GetTableClient("Customers");

        var customer = new TableEntity("CustomerPartition", orchestration.RowKey)
        {
            { "Name", orchestration["CustomerName"] },
            { "Email", orchestration["CustomerEmail"] }
        };
        await customerClient.AddEntityAsync(customer);

        orchestration["Status"] = "CustomerCreated";
        
        await _orchestrationClient.UpdateEntityAsync(orchestration, ETag.All, TableUpdateMode.Replace);

        await SendSagaMessageAsync(orchestration.RowKey, "CustomerCreated");
        _logger.LogInformation($"Customer created for saga ID: {orchestration.RowKey}");
    }

    private async Task ProcessCreateSubscriptionAsync(TableEntity orchestration)
    {
        //throw new ApplicationException("Test ex");
        // Process Create Subscription
        // ... (Add your subscription creation logic here)

        orchestration["Status"] = "SubscriptionCreated";
        
        await _orchestrationClient.UpdateEntityAsync(orchestration, ETag.All, TableUpdateMode.Replace);

        await SendSagaMessageAsync(orchestration.RowKey, "SubscriptionCreated");
        _logger.LogInformation($"Subscription created for saga ID: {orchestration.RowKey}");
    }

    private async Task ProcessStartPipelineAndSendEmailAsync(TableEntity orchestration)
    {
        await Task.WhenAll(
            ProcessStartPipelineAsync(orchestration),
            ProcessSendEmailAsync(orchestration)
        );

        orchestration["Status"] = "PipelineStartedAndEmailSent";

        await _orchestrationClient.UpdateEntityAsync(orchestration, ETag.All, TableUpdateMode.Replace);

        await SendSagaMessageAsync(orchestration.RowKey, "PipelineStartedAndEmailSent");
    }

    private async Task ProcessStartPipelineAsync(TableEntity orchestration)
    {
        if (orchestration["PipelineStatus"] == "Started")
        {
            return;
        }

        // Logic to start pipeline
        _logger.LogInformation($"Starting pipeline for {orchestration["CustomerName"]}");

        orchestration["PipelineStatus"] = "Started";
        
        await _orchestrationClient.UpdateEntityAsync(orchestration, ETag.All, TableUpdateMode.Replace);
    }

    private async Task ProcessSendEmailAsync(TableEntity orchestration)
    {
        if (orchestration["EmailStatus"] == "Sent")
        {
            return;
        }

        // Logic to send email
        _logger.LogInformation($"Sending email to {orchestration["CustomerEmail"]}");

        orchestration["EmailStatus"] = "Sent";
        
        await _orchestrationClient.UpdateEntityAsync(orchestration, ETag.All, TableUpdateMode.Replace);
    }

    private async Task ProcessCloseRequestAsync(TableEntity orchestration)
    {
        orchestration["Status"] = "Closed";

        await _orchestrationClient.UpdateEntityAsync(orchestration, ETag.All, TableUpdateMode.Replace);

        _logger.LogInformation($"Saga ID {orchestration.RowKey} has been closed.");
    }

    private async Task SendSagaMessageAsync(string sagaId, string status)
    {
        var sagaMessageJson = JsonSerializer.Serialize(new SagaMessage
        {
            SagaId = sagaId,
            Status = status
        }, inputType: typeof(SagaMessage), context: SagaMessageSerializerContext.Default);

        var message = new ServiceBusMessage(sagaMessageJson)
        {
            MessageId = Guid.NewGuid().ToString()
        };

        await _serviceBusSender.SendMessageAsync(message);
    }

    private async Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error processing message");

        if (args.ErrorSource == ServiceBusErrorSource.ProcessMessageCallback)
        {
            var message = args.Exception.Message;
            SagaMessage sagaMessage = default;// message.Body.ToObjectFromJson<SagaMessage>();

            // Update the orchestration status to "Failed" and record the error details
            var orchestrationResults = await _orchestrationClient.GetEntityAsync<TableEntity>("Orchestration", sagaMessage.SagaId);

            var orchestration = orchestrationResults.Value;

            orchestration["Status"] = "Failed";
            orchestration["Errors"] = JsonSerializer.Serialize(new
            {
                args.Exception.Message,
                args.Exception.StackTrace
            });

            await _orchestrationClient.UpdateEntityAsync(orchestration, ETag.All, TableUpdateMode.Replace);
        }
    }
}
