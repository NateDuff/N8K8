using N8.Shared.Messaging;
using N8.Shared.Serializers;
using N8.Worker;
using N8.Worker.Saga;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

//builder.AddRabbitMQClient("messaging");
builder.AddAzureServiceBusClient("messaging");
builder.AddAzureTableClient("tables");

//builder.Services.AddSingleton<RabbitMqPublisher>();
//builder.Services.AddSingleton<ServiceBusPublisher>();

//builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<SagaOrchestrator>();
builder.Services.AddHostedService<CustomerProvisioningWorker>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.TypeInfoResolverChain.Insert(1, CustomerRequestSerializerContext.Default);
    options.SerializerOptions.TypeInfoResolverChain.Insert(2, SagaMessageSerializerContext.Default);
    options.SerializerOptions.TypeInfoResolverChain.Insert(3, ServiceBusMessageSerializerContext.Default);
});


var host = builder.Build();
host.Run();
