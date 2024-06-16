
targetScope = 'subscription'

param applicationName string = 'dev'
param location string = 'northcentralus'
param kubernetesName string

var nodePoolBase = {
  name: 'system'
  count: 3
  vmSize: 'Standard_D4s_v4'
}

resource newRG 'Microsoft.Resources/resourceGroups@2021-01-01' = {
  name: 'rg-${applicationName}'
  location: location
}

module kubernetes './templates/aks-managed-cluster.bicep' = {
  scope: newRG
  name: 'kubernetes'
  params: {
    name: kubernetesName
    location: location
    networkPlugin: 'kubenet'
    systemPoolConfig: union(
      { name: 'npsystem', mode: 'System' },
      nodePoolBase
    )
    dnsPrefix: kubernetesName
  }
}
