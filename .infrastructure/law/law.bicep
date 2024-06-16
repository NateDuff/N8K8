param environmentName string

@description('The location used for the log analytics workspace')
param location string = resourceGroup().location

@description('Tags that will be applied to the log analytics workspace')
param tags object = {}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'law-${environmentName}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  tags: tags
}

output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
