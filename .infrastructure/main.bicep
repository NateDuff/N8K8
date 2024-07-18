targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention, the name of the resource group for your application will use this name, prefixed with rg-')
param environmentName string

@minLength(1)
@description('The location used for all deployed resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string = ''

@secure()
param messaging string

var tags = {
  'azd-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module resources 'resources.bicep' = {
  scope: rg
  name: 'resources'
  params: {
    location: location
    tags: tags
    principalId: principalId
    environmentName: environmentName
  }
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  scope: rg
  name: 'mi-${environmentName}'
}

module kubernetes './templates/aks-managed-cluster.bicep' = {
  scope: rg
  name: 'kubernetes'
  params: {
    applicationName: environmentName
    location: location
    msiClientId: managedIdentity.properties.clientId
    msiObjectId: managedIdentity.properties.principalId
    msiResourceId: managedIdentity.id
    logAnalyticsWorkspaceResourceID: law.outputs.logAnalyticsWorkspaceId
  }
}

module ai 'ai/app-insights.bicep' = {
  name: 'ai'
  scope: rg
  params: {
    environmentName: environmentName
    location: location
    logAnalyticsWorkspaceId: resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_ID
  }
}

module law 'law/law.bicep' = {
  name: 'law'
  scope: rg
  params: {
    environmentName: environmentName
    location: location
  }
}

module secrets 'secrets/secrets.module.bicep' = {
  name: 'secrets'
  scope: rg
  params: {
    location: location
    principalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    messaging: messaging
    environmentName: environmentName
  }
}

module storage 'storage/storage.module.bicep' = {
  name: 'storage'
  scope: rg
  params: {
    location: location
    principalId: resources.outputs.MANAGED_IDENTITY_PRINCIPAL_ID
    principalType: 'ServicePrincipal'
    environmentName: environmentName
  }
}
output MANAGED_IDENTITY_CLIENT_ID string = resources.outputs.MANAGED_IDENTITY_CLIENT_ID
output MANAGED_IDENTITY_NAME string = resources.outputs.MANAGED_IDENTITY_NAME
output AZURE_LOG_ANALYTICS_WORKSPACE_NAME string = resources.outputs.AZURE_LOG_ANALYTICS_WORKSPACE_NAME
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = resources.outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT
output AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID string = resources.outputs.AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_ID
output AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN string = resources.outputs.AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN

output AI_APPINSIGHTSCONNECTIONSTRING string = ai.outputs.appInsightsConnectionString

output SECRETS_VAULTURI string = secrets.outputs.vaultUri
output SECRETS_MESSAGINGURI string = secrets.outputs.messagingSecretUri
output STORAGE_TABLEENDPOINT string = storage.outputs.tableEndpoint
