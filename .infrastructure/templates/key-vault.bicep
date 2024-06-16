param name string

resource keyVault 'Microsoft.KeyVault/vaults@2019-09-01' existing = {
  name: name

  resource emailServiceClientSecretSecret 'secrets@2019-09-01' existing = {
    name: 'EmailServiceClientSecret'
  }
}

output vaultUri string = keyVault.properties.vaultUri
output emailServiceSecretUri string = keyVault::emailServiceClientSecretSecret.properties.secretUri
