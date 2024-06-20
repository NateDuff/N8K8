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
    private readonly ServiceBusSender _serviceBusSender;
    private readonly ServiceBusSender _progressBusSender;
    private readonly ILogger<SagaOrchestrator> _logger;
    private const string provisioningQueue = "newcustomer";

    public SagaOrchestrator(TableServiceClient tableServiceClient, ServiceBusClient serviceBusClient, ILogger<SagaOrchestrator> logger)
    {
        _logger = logger;
        _serviceBusClient = serviceBusClient;
        _tableServiceClient = tableServiceClient;

        _orchestrationClient = _tableServiceClient.GetTableClient("Orchestrations");

        _serviceBusSender = _serviceBusClient.CreateSender(provisioningQueue);
        _progressBusSender = _serviceBusClient.CreateSender("ha");

        _stateMachine = new StateMachine<SagaState, SagaTrigger>(SagaState.Start);

        ConfigureStateMachine();
    }

    /// <summary>
    /// Configures the state machine transitions.
    /// </summary>
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

    /// <summary>
    /// Processes the saga message from the queue & starts orchestration activties based on the current state.
    /// </summary>
    /// <param name="sagaMessage"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task ProcessAsync(SagaMessage sagaMessage, CancellationToken cancellationToken = default)
    {
        var orchestration = await _orchestrationClient.GetEntityAsync<TableEntity>("Orchestration", sagaMessage.SagaId, cancellationToken: cancellationToken);

        try
        {
            switch (_stateMachine.State)
            {
                case SagaState.Start:
                    await ProcessCreateCustomerAsync(orchestration, cancellationToken: cancellationToken);
                    break;

                case SagaState.CustomerCreated:
                    await ProcessCreateSubscriptionAsync(orchestration, cancellationToken: cancellationToken);
                    break;

                case SagaState.SubscriptionCreated:
                    await ProcessStartPipelineAndSendEmailAsync(orchestration, cancellationToken: cancellationToken);
                    break;

                case SagaState.PipelineStartedAndEmailSent:
                    await ProcessCloseRequestAsync(orchestration, cancellationToken: cancellationToken);
                    break;

                case SagaState.Closed:
                    _logger.LogInformation("Saga is already closed.");
                    break;

                default:
                    _logger.LogError("Invalid saga state {State}.", _stateMachine.State);
                    throw new InvalidOperationException("Invalid saga state.");
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(orchestration, ex, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Updates the orchestration entity in the table storage and sends the next message to the queue.
    /// </summary>
    /// <param name="orchestration"></param>
    /// <returns></returns>
    private async Task UpdateOrchestrationAsync(TableEntity orchestration, CancellationToken cancellationToken = default)
    {
        await _orchestrationClient.UpdateEntityAsync(orchestration, ETag.All, TableUpdateMode.Replace, cancellationToken: cancellationToken);

        if ((string)orchestration["Status"] == SagaState.Closed.ToString())
        {
            return;
        }

        // Send the next message to the queue
        var sagaMessage = new SagaMessage
        {
            SagaId = orchestration.RowKey,
            Status = orchestration["Status"].ToString() ?? throw new InvalidOperationException("Status is not set.")
        };

        var sagaMessageJson = JsonSerializer.Serialize(sagaMessage, SagaMessageSerializerContext.Default.SagaMessage);
        var message = new ServiceBusMessage(sagaMessageJson)
        {
            MessageId = Guid.NewGuid().ToString()
        };

        await _serviceBusSender.SendMessageAsync(message, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Handles an error that occurred while processing the orchestration.
    /// </summary>
    /// <param name="orchestration"></param>
    /// <param name="ex"></param>
    /// <returns></returns>
    private async Task HandleErrorAsync(TableEntity orchestration, Exception ex, CancellationToken cancellationToken = default)
    {
        _logger.LogError(ex, "An error occurred while processing the orchestration.");

        _stateMachine.Fire(SagaTrigger.Error);

        orchestration["Status"] = _stateMachine.State.ToString();
        orchestration["Errors"] = JsonSerializer.Serialize(new { ex.Message, ex.StackTrace }, SagaMessageSerializerContext.Default.SagaMessage);

        await UpdateOrchestrationAsync(orchestration, cancellationToken);
    }
}