namespace N8.Shared.Saga;


public record CustomerRequest(string CustomerName, string CustomerEmail, string SubscriptionName);


public record SagaMessage
{
    public string SagaId { get; set; }
    public string Status { get; set; }
}
