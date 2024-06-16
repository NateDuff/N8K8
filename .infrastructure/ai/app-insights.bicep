param environmentName string
param location string = 'northcentralus'
param logAnalyticsWorkspaceId string

resource appInsights 'Microsoft.Insights/components@2020-02-02-preview' = {
  name: 'ai-${environmentName}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspaceId
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    Flow_Type: 'Bluefield'
    Request_Source: 'rest'
    DisableIpMasking: true
  }
}

output appInsightsName string = appInsights.properties.Name
output appInsightsConnectionString string = appInsights.properties.ConnectionString
