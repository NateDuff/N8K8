# Getting Started

## Prerequesits

1. Dotnet 8 SDK & Aspire Workloads
2. Node v.20 / Latest
3. Docker Desktop with Kubernetes Enabled

## Build
```powershell
## Builds container images
.\build.ps1
```

## Run (K8s)
```powershell
## Starts all deployments & services
.\run.ps1
```

## Delete K8s Resources
```powershell
## Deletes everything in K8s
kubectl delete all --all
```

# Architecture

## Web
- Interactive Blazor Server application used to demonstrate tailwindcss UI components, API interactivity, message consumption via rabbitMQ, & real time server/client communication via SignalR.

## API
- Minimal API with sample TODO's endpoint.

## Worker
- Background service publishing RabbitMQ messages every 3 seconds.

## Jobs
- On demand kubernetes job examples for a dotnet console & Azure PowerShell application.

## Others

### Aspire
- Used for local development of micro-services.

### K8s
- Houses all kubernetes deployment & service YAML definitions
