using Azure.Data.Tables;
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
        await UpdateOrchestrationAsync(orchestration, cancellationToken: cancellationToken);
    }

    private async Task ProcessCreateSubscriptionAsync(TableEntity orchestration, CancellationToken cancellationToken = default)
    {
        // Your logic to create subscription
        _stateMachine.Fire(SagaTrigger.CreateSubscription);
        orchestration["Status"] = _stateMachine.State.ToString();
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
    }

    private async Task ProcessCloseRequestAsync(TableEntity orchestration, CancellationToken cancellationToken = default)
    {
        // Your logic to close request
        _stateMachine.Fire(SagaTrigger.Close);
        orchestration["Status"] = _stateMachine.State.ToString();
        await UpdateOrchestrationAsync(orchestration, cancellationToken: cancellationToken);
    }
}
