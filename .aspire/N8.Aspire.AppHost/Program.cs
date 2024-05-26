var builder = DistributedApplication.CreateBuilder(args);

//var messaging = builder.AddRabbitMQ("messaging");
var messaging = builder.AddConnectionString("messaging");

//var storage = builder.AddAzureStorage("storage");

//storage.RunAsEmulator();

//var tables = storage.AddTables("tables");

var tables = builder.AddConnectionString("tables");

var api = builder.AddProject<Projects.N8_API>("n8-api")
    .WithReference(tables);

builder.AddProject<Projects.N8_Web>("n8-web")
    .WithReference(messaging)
    .WithReference(api)
    .WithReference(tables);
//.WaitFor(messaging);

builder.AddProject<Projects.N8_Worker>("n8-worker")
    .WithReference(messaging)
    .WithReference(tables);
    //.WaitFor(messaging);

builder.Build().Run();
