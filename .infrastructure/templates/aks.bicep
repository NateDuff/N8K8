@secure()
param kubeConfig string

provider kubernetes with {
  namespace: 'default'
  kubeConfig: kubeConfig
}

resource appsDeployment_n8Api 'apps/Deployment@v1' = {
  metadata: {
    name: 'n8-api'
    labels: {
      app: 'n8-api'
    }
  }
  spec: {
    minReadySeconds: 60
    replicas: 1
    selector: {
      matchLabels: {
        app: 'n8-api'
      }
    }
    strategy: {
      type: 'Recreate'
    }
    template: {
      metadata: {
        labels: {
          app: 'n8-api'
        }
      }
      spec: {
        containers: [
          {
            name: 'n8-api'
            image: 'acrn8k8s6.azurecr.io/n8-api:latest'
            imagePullPolicy: 'Always'
            ports: [
              {
                name: 'http'
                containerPort: 8080
              }
              {
                name: 'https'
                containerPort: 8443
              }
            ]
            envFrom: [
              {
                configMapRef: {
                  name: 'n8-api-env'
                }
              }
              {
                secretRef: {
                  name: 'n8-api-secrets'
                }
              }
            ]
          }
        ]
        terminationGracePeriodSeconds: 180
      }
    }
  }
}

resource coreService_n8Api 'core/Service@v1' = {
  metadata: {
    name: 'n8-api'
  }
  spec: {
    type: 'ClusterIP'
    selector: {
      app: 'n8-api'
    }
    ports: [
      {
        name: 'http'
        port: 80
        targetPort: 8080
      }
      {
        name: 'https'
        port: 443
        targetPort: 8443
      }
    ]
  }
}

resource appsDeployment_n8Web 'apps/Deployment@v1' = {
  metadata: {
    name: 'n8-web'
    labels: {
      app: 'n8-web'
    }
  }
  spec: {
    minReadySeconds: 60
    replicas: 1
    selector: {
      matchLabels: {
        app: 'n8-web'
      }
    }
    strategy: {
      type: 'Recreate'
    }
    template: {
      metadata: {
        labels: {
          app: 'n8-web'
        }
      }
      spec: {
        containers: [
          {
            name: 'n8-web'
            image: 'acrn8k8s6.azurecr.io/n8-web:latest'
            imagePullPolicy: 'Always'
            ports: [
              {
                name: 'http'
                containerPort: 8080
              }
              {
                name: 'https'
                containerPort: 8443
              }
            ]
            envFrom: [
              {
                configMapRef: {
                  name: 'n8-web-env'
                }
              }
              {
                secretRef: {
                  name: 'n8-web-secrets'
                }
              }
            ]
          }
        ]
        terminationGracePeriodSeconds: 180
      }
    }
  }
}

resource coreService_n8Web 'core/Service@v1' = {
  metadata: {
    name: 'n8-web'
  }
  spec: {
    type: 'ClusterIP'
    selector: {
      app: 'n8-web'
    }
    ports: [
      {
        name: 'http'
        port: 80
        targetPort: 8080
      }
      {
        name: 'https'
        port: 443
        targetPort: 8443
      }
    ]
  }
}

resource appsDeployment_n8Worker 'apps/Deployment@v1' = {
  metadata: {
    name: 'n8-worker'
    labels: {
      app: 'n8-worker'
    }
  }
  spec: {
    minReadySeconds: 60
    replicas: 1
    selector: {
      matchLabels: {
        app: 'n8-worker'
      }
    }
    strategy: {
      type: 'Recreate'
    }
    template: {
      metadata: {
        labels: {
          app: 'n8-worker'
        }
      }
      spec: {
        containers: [
          {
            name: 'n8-worker'
            image: 'acrn8k8s6.azurecr.io/n8-worker:latest'
            imagePullPolicy: 'Always'
            envFrom: [
              {
                configMapRef: {
                  name: 'n8-worker-env'
                }
              }
              {
                secretRef: {
                  name: 'n8-worker-secrets'
                }
              }
            ]
          }
        ]
        terminationGracePeriodSeconds: 180
      }
    }
  }
}

resource coreService_n8WebLb 'core/Service@v1' = {
  metadata: {
    name: 'n8-web-lb'
  }
  spec: {
    ports: [
      {
        name: 'http'
        port: 80
        targetPort: 8080
      }
      {
        name: 'https'
        port: 443
        targetPort: 8443
      }
    ]
    selector: {
      app: 'n8-web'
    }
    type: 'LoadBalancer'
  }
}

resource coreService_n8ApiLb 'core/Service@v1' = {
  metadata: {
    name: 'n8-api-lb'
  }
  spec: {
    ports: [
      {
        name: 'http'
        port: 80
        targetPort: 8081
      }
      {
        name: 'https'
        port: 443
        targetPort: 8444
      }
    ]
    selector: {
      app: 'n8-api'
    }
    type: 'LoadBalancer'
  }
}
