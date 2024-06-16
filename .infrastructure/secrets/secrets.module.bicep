targetScope = 'resourceGroup'

param environmentName string

@description('')
param location string = resourceGroup().location

@description('')
param principalId string

@secure()
param messaging string

var accessPolicies = [
  {
    objectId: principalId
    permissions: {
      secrets: [
        'all'
      ]
      keys: [
        'all'
      ]
      certificates: [
        'all'
      ]
    }
    tenantId: subscription().tenantId
  }
  {
    objectId: 'a998a3f0-0995-4121-a76a-ea86ac7423df'
    permissions: {
      secrets: [
        'all'
      ]
      keys: [
        'all'
      ]
      certificates: [
        'all'
      ]
    }
    tenantId: subscription().tenantId
  }
]

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: 'kv-${environmentName}'
  location: location
  tags: {
    'aspire-resource-name': 'secrets'
  }
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: accessPolicies
  }

  resource messagingSecret 'secrets@2023-07-01' = {
    name: 'connectionstrings--messaging'
    properties: {
      value: messaging
    }
  }
}

output vaultUri string = keyVault.properties.vaultUri
output messagingSecretUri string = keyVault::messagingSecret.properties.secretUri
