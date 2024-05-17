var builder = DistributedApplication.CreateBuilder(args);

var messaging = builder.AddRabbitMQ("messaging");

var api = builder.AddProject<Projects.N8_API>("n8-api");

builder.AddProject<Projects.N8_Web>("n8-web")
    .WithReference(messaging)
    .WithReference(api);
//.WaitFor(messaging);

builder.AddProject<Projects.N8_Worker>("n8-worker")
    .WithReference(messaging);
    //.WaitFor(messaging);

builder.Build().Run();
