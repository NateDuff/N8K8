using Microsoft.Extensions.Hosting;
using N8.Aspire.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

// Parameters
var messaging = builder.AddParameter("messaging", true);
var envName = builder.AddParameter("environmentName");

// Services
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
    .WithReference(tables)
    .WithReference(secrets)
    .WithExternalHttpEndpoints();

var web = builder.AddProject<Projects.N8_Web>("n8-web")
    .WithReference(secrets)
    .WithReference(api)
    .WithReference(tables)
    .WithExternalHttpEndpoints();

var worker = builder.AddProject<Projects.N8_Worker>("n8-worker")
    .WithReference(secrets)
    .WithReference(tables);

if (builder.Environment.IsProduction())
{
    var appInsightsConnection = builder.AddBicepAppInsightsTemplate(envName);

    api.WithAppInsights(appInsightsConnection);
    web.WithAppInsights(appInsightsConnection);
    worker.WithAppInsights(appInsightsConnection);
}

builder.Build().Run();
