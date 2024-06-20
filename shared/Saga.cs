using System.ComponentModel.DataAnnotations;

namespace N8.Shared.Saga;

public class CustomerRequest
{
    [Required]
    public string CustomerName { get; set; }

    [Required]
    public string CustomerEmail { get; set; }

    [Required]
    public string SubscriptionName { get; set; }
}

public record SagaMessage
{
    public required string SagaId { get; set; }
    public required string Status { get; set; }
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
