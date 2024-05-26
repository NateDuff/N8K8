using Azure;
using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using N8.Shared.Saga;
using N8.Shared.Serializers;
using Stateless;
using System.Text.Json;

namespace N8.Worker.Saga;

public partial class SagaOrchestrator
{
    private readonly StateMachine<SagaState, SagaTrigger> _stateMachine;
    private readonly TableServiceClient _tableServiceClient;
    private readonly TableClient _orchestrationClient;
    private readonly ServiceBusClient _serviceBusClient;
    private ServiceBusSender _serviceBusSender;
    private readonly ILogger<SagaOrchestrator> _logger;
    private const string provisioningQueue = "newcustomer";

    public SagaOrchestrator(TableServiceClient tableServiceClient, ServiceBusClient serviceBusClient, ILogger<SagaOrchestrator> logger)
    {
        _logger = logger;
        _serviceBusClient = serviceBusClient;
        _tableServiceClient = tableServiceClient;

        _orchestrationClient = _tableServiceClient.GetTableClient("Orchestrations");
        _serviceBusSender = _serviceBusClient.CreateSender(provisioningQueue);

        _stateMachine = new StateMachine<SagaState, SagaTrigger>(SagaState.Start);

        ConfigureStateMachine();
    }

    private void ConfigureStateMachine()
    {
        _stateMachine.OnTransitioned(transition =>
        {
            _logger.LogInformation("Transitioned from {From} to {To} via {Trigger}.", transition.Source, transition.Destination, transition.Trigger);
        });

        // Normal states
        _stateMachine.Configure(SagaState.Start)
            .Permit(SagaTrigger.CreateCustomer, SagaState.CustomerCreated);

        _stateMachine.Configure(SagaState.CustomerCreated)
            .Permit(SagaTrigger.CreateSubscription, SagaState.SubscriptionCreated);

        _stateMachine.Configure(SagaState.SubscriptionCreated)
            .Permit(SagaTrigger.StartPipelineAndSendEmail, SagaState.PipelineStartedAndEmailSent);

        _stateMachine.Configure(SagaState.PipelineStartedAndEmailSent)
            .Permit(SagaTrigger.Close, SagaState.Closed);

        // Failed states
        _stateMachine.Configure(SagaState.Start)
            .Permit(SagaTrigger.Error, SagaState.Failed);

        _stateMachine.Configure(SagaState.CustomerCreated)
            .Permit(SagaTrigger.Error, SagaState.Failed);

        _stateMachine.Configure(SagaState.SubscriptionCreated)
            .Permit(SagaTrigger.Error, SagaState.Failed);

        _stateMachine.Configure(SagaState.PipelineStartedAndEmailSent)
            .Permit(SagaTrigger.Error, SagaState.Failed);
    }

    public async Task ProcessAsync(SagaMessage sagaMessage)
    {
        var orchestration = await _orchestrationClient.GetEntityAsync<TableEntity>("Orchestration", sagaMessage.SagaId);

        switch (_stateMachine.State)
        {
            case SagaState.Start:
                await ProcessCreateCustomerAsync(orchestration);
                break;

            case SagaState.CustomerCreated:
                await ProcessCreateSubscriptionAsync(orchestration);
                break;

            case SagaState.SubscriptionCreated:
                await ProcessStartPipelineAndSendEmailAsync(orchestration);
                break;

            case SagaState.PipelineStartedAndEmailSent:
                await ProcessCloseRequestAsync(orchestration);
                break;

            default:
                _logger.LogError("Invalid saga state {State}.", nameof(_stateMachine.State));
                throw new InvalidOperationException("Invalid saga state.");
        }
    }

    private async Task UpdateOrchestrationAsync(TableEntity orchestration)
    {
        await _orchestrationClient.UpdateEntityAsync(orchestration, ETag.All, TableUpdateMode.Replace);

        if (_stateMachine.State == SagaState.Closed)
        {
            return;
        }

        // Send the next message to the queue
        var sagaMessage = new SagaMessage
        {
            SagaId = orchestration.RowKey,
            Status = orchestration["Status"].ToString()
        };

        var sagaMessageJson = JsonSerializer.Serialize(sagaMessage, SagaMessageSerializerContext.Default.SagaMessage);
        var message = new ServiceBusMessage(sagaMessageJson)
        {
            MessageId = Guid.NewGuid().ToString()
        };

        await _serviceBusSender.SendMessageAsync(message);
    }

    private async Task HandleErrorAsync(TableEntity orchestration, Exception ex)
    {
        _logger.LogError(ex, "An error occurred while processing the orchestration.");

        _stateMachine.Fire(SagaTrigger.Error);

        orchestration["Status"] = _stateMachine.State.ToString();
        orchestration["Errors"] = JsonSerializer.Serialize(new { ex.Message, ex.StackTrace }, SagaMessageSerializerContext.Default.SagaMessage);

        await UpdateOrchestrationAsync(orchestration);
    }
}