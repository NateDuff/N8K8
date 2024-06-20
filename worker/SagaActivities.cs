using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using N8.Shared.Saga;

namespace N8.Worker.Saga;

public partial class SagaOrchestrator
{
    private async Task ProcessCreateCustomerAsync(TableEntity orchestration, CancellationToken cancellationToken = default)
    {
        // Create customer
        var customerClient = _tableServiceClient.GetTableClient("Customers");

        //customerClient.Create();

        var customer = new TableEntity("CustomerPartition", orchestration.RowKey)
        {
            { "Name", orchestration["CustomerName"] },
            { "Email", orchestration["CustomerEmail"] }
        };

        await customerClient.AddEntityAsync(customer, cancellationToken: cancellationToken);

        // Your logic to create customer
        _stateMachine.Fire(SagaTrigger.CreateCustomer);
        orchestration["Status"] = _stateMachine.State.ToString();

        await _progressBusSender.SendMessageAsync(new ServiceBusMessage("Step 1: Customer Record Created"), cancellationToken: cancellationToken);

        await UpdateOrchestrationAsync(orchestration, cancellationToken: cancellationToken);
    }

    private async Task ProcessCreateSubscriptionAsync(TableEntity orchestration, CancellationToken cancellationToken = default)
    {
        // Your logic to create subscription
        _stateMachine.Fire(SagaTrigger.CreateSubscription);
        orchestration["Status"] = _stateMachine.State.ToString();

        await _progressBusSender.SendMessageAsync(new ServiceBusMessage("Step 2: Customer Subscription Created"), cancellationToken: cancellationToken);

        await UpdateOrchestrationAsync(orchestration, cancellationToken: cancellationToken);
    }

    private async Task ProcessStartPipelineAndSendEmailAsync(TableEntity orchestration, CancellationToken cancellationToken = default)
    {
        await Task.WhenAll(
            ProcessStartPipelineAsync(orchestration, cancellationToken: cancellationToken),
            ProcessSendEmailAsync(orchestration, cancellationToken: cancellationToken)
        );

        _stateMachine.Fire(SagaTrigger.StartPipelineAndSendEmail);
        orchestration["Status"] = _stateMachine.State.ToString();
        await UpdateOrchestrationAsync(orchestration, cancellationToken: cancellationToken);
    }

    private async Task ProcessStartPipelineAsync(TableEntity orchestration, CancellationToken cancellationToken = default)
    {
        if ((string)orchestration["PipelineStatus"] == "Started")
        {
            return;
        }

        // Logic to start pipeline
        _logger.LogInformation("Starting pipeline for {CustomerName}", orchestration["CustomerName"]);

        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken: cancellationToken);

        orchestration["PipelineStatus"] = "Started";

        await _progressBusSender.SendMessageAsync(new ServiceBusMessage("Step 3A: Customer Onboarding Pipeline Started"), cancellationToken: cancellationToken);
    }

    private async Task ProcessSendEmailAsync(TableEntity orchestration, CancellationToken cancellationToken = default)
    {
        if ((string)orchestration["EmailStatus"] == "Sent")
        {
            return;
        }

        // Logic to send email
        _logger.LogInformation("Sending email to {CustomerName}", orchestration["CustomerEmail"]);

        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken: cancellationToken);

        orchestration["EmailStatus"] = "Sent";

        await _progressBusSender.SendMessageAsync(new ServiceBusMessage("Step 3B: Customer Email Sent"), cancellationToken: cancellationToken);
    }

    private async Task ProcessCloseRequestAsync(TableEntity orchestration, CancellationToken cancellationToken = default)
    {
        // Your logic to close request
        _stateMachine.Fire(SagaTrigger.Close);
        orchestration["Status"] = _stateMachine.State.ToString();

        await _progressBusSender.SendMessageAsync(new ServiceBusMessage("Step 4: Orchestration Complete"), cancellationToken: cancellationToken);

        await UpdateOrchestrationAsync(orchestration, cancellationToken: cancellationToken);
    }
}
