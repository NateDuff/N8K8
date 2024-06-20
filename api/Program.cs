using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using N8.Shared;
using N8.Shared.Saga;
using N8.Shared.Serializers;
using System.Text.Json;

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();

builder.Configuration.AddAzureKeyVaultSecrets("secrets");
builder.AddAzureServiceBusClient("messaging");
builder.AddAzureTableClient("tables");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.TypeInfoResolverChain.Insert(1, CustomerRequestSerializerContext.Default);
    options.SerializerOptions.TypeInfoResolverChain.Insert(2, SagaMessageSerializerContext.Default);
    options.SerializerOptions.TypeInfoResolverChain.Insert(3, ServiceBusMessageSerializerContext.Default);
});

if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var configuration = builder.Configuration;

var app = builder.Build();

var sampleTodos = new Todo[] {
    new(1, "Walk the dog"),
    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
    new(4, "Clean the bathroom"),
    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
};

var todosApi = app.MapGroup("/todos")
    .WithOpenApi();

todosApi.MapGet("/", () => sampleTodos);
todosApi.MapGet("/{id}", (int id) =>
    sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

app.MapPut("/start-provisioning", async (CustomerRequest request, ServiceBusClient serviceBusClient, TableServiceClient tableServiceClient, ILogger<Program> logger) =>
{
    var tableClient = tableServiceClient.GetTableClient("Orchestrations");

    //await tableClient.CreateAsync();

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

    logger.LogInformation("Started provisioning saga with ID: {SagaId}", sagaId);

    return Results.Accepted();
})
.WithOpenApi();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    
    // Create Orchestrations & Customers tables
    var tableServiceClient = app.Services.GetRequiredService<TableServiceClient>();

    var tableClient = tableServiceClient.GetTableClient("Orchestrations");
    await tableClient.CreateIfNotExistsAsync();

    tableClient = tableServiceClient.GetTableClient("Customers");
    await tableClient.CreateIfNotExistsAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/config", (IConfiguration configuration) => configuration.AsEnumerable())
.WithOpenApi();

// List orchestrations endpoint
app.MapGet("/orchestrations", async (TableServiceClient tableServiceClient) =>
{
    var tableClient = tableServiceClient.GetTableClient("Orchestrations");

    //await tableClient.CreateAsync();

    var orchestrations = new List<TableEntity>();

    await foreach (var orchestration in tableClient.QueryAsync<TableEntity>())
    {
        orchestrations.Add(orchestration);
    }

    return orchestrations;
})
.WithOpenApi();

// List Customers endpoint
app.MapGet("/customers", async (TableServiceClient tableServiceClient) =>
{
    var tableClient = tableServiceClient.GetTableClient("Customers");

    //await tableClient.CreateAsync();

    var customers = new List<TableEntity>();

    await foreach (var customer in tableClient.QueryAsync<TableEntity>())
    {
        customers.Add(customer);
    }

    return customers;
})
.WithOpenApi();

app.MapDefaultEndpoints();

app.Run();
