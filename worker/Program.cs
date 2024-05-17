using N8.Shared.Messaging;
using N8.Worker;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    return new ConnectionFactory()
    {
        HostName = builder.Configuration["RABBITMQ_HOST"],
        UserName = "guest",
        Password = "guest"
    };
});

builder.Services.AddSingleton<RabbitMqPublisher>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
