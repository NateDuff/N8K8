param applicationName string

@description('The location of the Managed Cluster resource.')
param location string = resourceGroup().location

@description('Disk size (in GB) to provision for each of the agent pool nodes. This value ranges from 0 to 1023. Specifying 0 will apply the default disk size for that agentVMSize.')
@minValue(0)
@maxValue(1023)
param osDiskSizeGB int = 0

@description('The number of nodes for the cluster.')
@minValue(1)
@maxValue(50)
param agentCount int = 1

@description('The size of the Virtual Machine.')
param agentVMSize string = 'Standard_A2_v2'

param msiResourceId string
param msiClientId string
param msiObjectId string

param logAnalyticsWorkspaceResourceID string

resource aks 'Microsoft.ContainerService/managedClusters@2024-03-02-preview' = {
  name: 'aks-${applicationName}'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${msiResourceId}': {}
    }
  }
  sku: {
    name: 'Base'
    tier: 'Free'
  }
  properties: {
    supportPlan: 'KubernetesOfficial'
    dnsPrefix: 'aks-${applicationName}-dns'
    agentPoolProfiles: [
      {
        name: 'agentpool'
        osDiskSizeGB: osDiskSizeGB
        count: agentCount
        vmSize: agentVMSize
        osType: 'Linux'
        osSKU: 'Ubuntu'
        mode: 'System'
        minCount: 2
        maxCount: 3
        maxPods: 110
        enableAutoScaling: true
      }
    ]
    oidcIssuerProfile: {
      enabled: true
    }
    disableLocalAccounts: true
    nodeResourceGroup: 'rg-${applicationName}-nodes'
    enableRBAC: true
    aadProfile: {
      managed: true
      enableAzureRBAC: true
      adminGroupObjectIDs: [
        '25869ba3-c562-48d0-99e0-a331610cafdf'
      ]
      tenantID: tenant().tenantId
    }
    servicePrincipalProfile: {
      clientId: 'msi'
    }
    azureMonitorProfile: {
      containerInsights: {
        enabled: true
        logAnalyticsWorkspaceResourceId: logAnalyticsWorkspaceResourceID
      }
    }
    networkProfile: {
      networkPlugin: 'azure'
      networkDataplane: 'azure'
      loadBalancerSku: 'standard'
      serviceCidr: '10.0.0.0/16'
      dnsServiceIP: '10.0.0.10'
    }
    identityProfile: {
      kubeletidentity: {
        resourceId: msiResourceId
        clientId: msiClientId
        objectId: msiObjectId
      }
    }
    windowsProfile: {
      adminUsername: 'azureuser'
      enableCSIProxy: true
    }
    addonProfiles: {
      aciConnectorLinux: {
        enabled: true
        config: {
          subnetName: 'virtual-node-aci'
        }
      }
      omsAgent: {
        enabled: true
        config: {
          logAnalyticsWorkspaceResourceID: logAnalyticsWorkspaceResourceID
          useAADAuth: 'true'
        }
      }
    }
  }
}

output controlPlaneFQDN string = aks.properties.fqdn
