using Aspire.Hosting.Azure;

namespace N8.Aspire.AppHost.Extensions;

public static class BuilderExtensions
{
    public static BicepOutputReference AddBicepAppInsightsTemplate(this IDistributedApplicationBuilder builder,
        IResourceBuilder<ParameterResource> envName)
    {
        if (builder.ExecutionContext.IsPublishMode)
        {
            var law = builder.AddBicepTemplate("law", "../../.infrastructure/law/law.bicep")
                .WithParameter("environmentName", envName);

            return builder.AddBicepTemplate("ai", "../../.infrastructure/ai/app-insights.bicep")
                .WithParameter(AzureBicepResource.KnownParameters.LogAnalyticsWorkspaceId)
                .WithParameter("environmentName", envName)
                .GetOutput("appInsightsConnectionString");
        }

        return builder.AddBicepTemplate("ai", "../../.infrastructure/ai/app-insights.bicep")
            .WithParameter("logAnalyticsWorkspaceId", builder.AddParameter("laWorkspaceId"))
            .WithParameter("environmentName", envName)
            .GetOutput("appInsightsConnectionString");
    }

    public static IResourceBuilder<T> WithAppInsights<T>(this IResourceBuilder<T> builder, BicepOutputReference appInsightsConnection)
        where T : IResourceWithEnvironment
    {
        return builder.WithEnvironment("APPLICATIONINSIGHTS_CONNECTION_STRING", appInsightsConnection);
    }
}
