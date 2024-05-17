using N8.Shared.Messaging;
using N8.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddRabbitMQClient("messaging");

builder.Services.AddSingleton<RabbitMqPublisher>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
