# Getting Started

## Prerequesits

1. Dotnet 8 SDK & Aspire Workloads (minimum: 8.0.0-preview.7.24251.11/8.0.100)
2. Node v.16 Minimum
3. Docker Desktop with Kubernetes Enabled

## Setup
```powershell
## Install Aspire workload (update if necessary with dotnet workload update)
dotnet workload install aspire

## Restore packages
dotnet restore

## Web project NPM installs
CD .\web

npm install

npm run buildcss ## optional file watcher with npm run buildcss-watch

## User secret setup
CD ..\.aspire\N8.Aspire.AppHost\

dotnet user-secrets set MESSAGING "{{REDACTED}}"

```

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
- Houses all kubernetes deployment & service YAML definitions.
