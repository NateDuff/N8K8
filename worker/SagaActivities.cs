using Azure.Data.Tables;
using N8.Shared.Saga;

namespace N8.Worker.Saga;

public partial class SagaOrchestrator
{
    private async Task ProcessCreateCustomerAsync(TableEntity orchestration)
    {
        try
        {
            // Create customer
            var customerClient = _tableServiceClient.GetTableClient("Customers");

            var customer = new TableEntity("CustomerPartition", orchestration.RowKey)
            {
                { "Name", orchestration["CustomerName"] },
                { "Email", orchestration["CustomerEmail"] }
            };

            await customerClient.AddEntityAsync(customer);

            // Your logic to create customer
            _stateMachine.Fire(SagaTrigger.CreateCustomer);
            orchestration["Status"] = _stateMachine.State.ToString();
            await UpdateOrchestrationAsync(orchestration);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(orchestration, ex);
        }
    }

    private async Task ProcessCreateSubscriptionAsync(TableEntity orchestration)
    {
        try
        {
            // Your logic to create subscription
            _stateMachine.Fire(SagaTrigger.CreateSubscription);
            orchestration["Status"] = _stateMachine.State.ToString();
            await UpdateOrchestrationAsync(orchestration);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(orchestration, ex);
        }
    }

    private async Task ProcessStartPipelineAndSendEmailAsync(TableEntity orchestration)
    {
        try
        {
            await Task.WhenAll(
                ProcessStartPipelineAsync(orchestration),
                ProcessSendEmailAsync(orchestration)
            );
            _stateMachine.Fire(SagaTrigger.StartPipelineAndSendEmail);
            orchestration["Status"] = _stateMachine.State.ToString();
            await UpdateOrchestrationAsync(orchestration);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(orchestration, ex);
        }
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
    }

    private async Task ProcessCloseRequestAsync(TableEntity orchestration)
    {
        try
        {
            // Your logic to close request
            _stateMachine.Fire(SagaTrigger.Close);
            orchestration["Status"] = _stateMachine.State.ToString();
            await UpdateOrchestrationAsync(orchestration);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(orchestration, ex);
        }
    }
}
