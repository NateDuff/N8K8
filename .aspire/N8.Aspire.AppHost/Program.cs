using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var messaging = builder.AddParameter("messaging", true);

var envName = builder.AddParameter("environmentName");

IResourceBuilder<AzureBicepResource> appInsights;

if (builder.ExecutionContext.IsPublishMode)
{
    var law = builder.AddBicepTemplate("law", "../../.infrastructure/law/law.bicep")
        .WithParameter("environmentName", envName);

    appInsights = builder.AddBicepTemplate("ai", "../../.infrastructure/ai/app-insights.bicep")
        .WithParameter(AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId)
        .WithParameter("environmentName", envName);
}
else
{
    appInsights = builder.AddBicepTemplate("ai", "../../.infrastructure/ai/app-insights.bicep")
        .WithParameter("logAnalyticsWorkspaceId", builder.AddParameter("laWorkspaceId"))
        .WithParameter("environmentName", envName);
}

var secrets = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureKeyVault("secrets")
        .WithParameter("messaging", messaging)
    : builder.AddConnectionString("secrets");

var tables = builder.AddAzureStorage("storage").RunAsEmulator(container =>
{
    container.WithDataBindMount();
}).AddTables("tables");

var api = builder.AddProject<Projects.N8_API>("n8-api")
    .WithEnvironment("APPLICATIONINSIGHTS_CONNECTION_STRING", appInsights.GetOutput("appInsightsConnectionString"))
    .WithReference(tables)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.N8_Web>("n8-web")
    .WithEnvironment("APPLICATIONINSIGHTS_CONNECTION_STRING", appInsights.GetOutput("appInsightsConnectionString"))
    .WithReference(secrets)
    .WithReference(api)
    .WithReference(tables)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.N8_Worker>("n8-worker")
    .WithEnvironment("APPLICATIONINSIGHTS_CONNECTION_STRING", appInsights.GetOutput("appInsightsConnectionString"))
    .WithReference(secrets)
    .WithReference(tables)
    .WithExternalHttpEndpoints();

builder.Build().Run();
