using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using N8.Shared;
using N8.Shared.Saga;
using N8.Shared.Serializers;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureServiceBusClient("messaging");
builder.AddAzureTableClient("tables");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.TypeInfoResolverChain.Insert(1, CustomerRequestSerializerContext.Default);
    options.SerializerOptions.TypeInfoResolverChain.Insert(2, SagaMessageSerializerContext.Default);
    options.SerializerOptions.TypeInfoResolverChain.Insert(3, ServiceBusMessageSerializerContext.Default);
});

var configuration = builder.Configuration;

var app = builder.Build();

var sampleTodos = new Todo[] {
    new(1, "Walk the dog"),
    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
    new(4, "Clean the bathroom"),
    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
};

var todosApi = app.MapGroup("/todos");
todosApi.MapGet("/", () => sampleTodos);
todosApi.MapGet("/{id}", (int id) =>
    sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

app.MapPost("/start-provisioning", async (CustomerRequest request, ServiceBusClient serviceBusClient, TableServiceClient tableServiceClient, ILogger<Program> logger) =>
{
    var tableClient = tableServiceClient.GetTableClient("Orchestrations");

    var sagaId = Guid.NewGuid().ToString();

    // Create a new orchestration record
    var orchestration = new TableEntity("Orchestration", sagaId)
    {
        { "CustomerName", request.CustomerName },
        { "CustomerEmail", request.CustomerEmail },
        { "SubscriptionName", request.SubscriptionName },
        { "Status", "New" }
    };
    await tableClient.AddEntityAsync(orchestration);

    // Send the initial message to start the saga
    var sagaMessageJson = JsonSerializer.Serialize(new SagaMessage
    {
        SagaId = sagaId,
        Status = "New"
    }, inputType: typeof(SagaMessage), context: SagaMessageSerializerContext.Default);

    var message = new ServiceBusMessage(sagaMessageJson)
    {
        MessageId = sagaId
    };

    await using var sender = serviceBusClient.CreateSender("newcustomer");
    await sender.SendMessageAsync(message);

    logger.LogInformation($"Started provisioning saga with ID: {sagaId}");

    return Results.Accepted();
});


app.MapDefaultEndpoints();

app.Run();
