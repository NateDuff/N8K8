using N8.Aspire.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

// Parameters
var messaging = builder.AddParameter("messaging", true);
var envName = builder.AddParameter("environmentName");

// Services
var appInsightsConnection = builder.AddBicepAppInsightsTemplate(envName);

var secrets = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureKeyVault("secrets")
        .WithParameter("messaging", messaging)
    : builder.AddConnectionString("secrets");

var tables = builder.AddAzureStorage("storage").RunAsEmulator(container =>
{
    container.WithDataBindMount();
}).AddTables("tables");

// Projects
var api = builder.AddProject<Projects.N8_API>("n8-api")
    .WithAppInsights(appInsightsConnection)
    .WithReference(tables)
    .WithReference(secrets)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.N8_Web>("n8-web")
    .WithAppInsights(appInsightsConnection)
    .WithReference(secrets)
    .WithReference(api)
    .WithReference(tables)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.N8_Worker>("n8-worker")
    .WithAppInsights(appInsightsConnection)
    .WithReference(secrets)
    .WithReference(tables);

builder.Build().Run();
