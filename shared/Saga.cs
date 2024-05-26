namespace N8.Shared.Saga;


public record CustomerRequest(string CustomerName, string CustomerEmail, string SubscriptionName);


public record SagaMessage
{
    public string SagaId { get; set; }
    public string Status { get; set; }
}

public enum SagaState
{
    Start,
    CustomerCreated,
    SubscriptionCreated,
    PipelineStartedAndEmailSent,
    Closed,
    Failed
}

public enum SagaTrigger
{
    CreateCustomer,
    CreateSubscription,
    StartPipelineAndSendEmail,
    Close,
    Error
}
