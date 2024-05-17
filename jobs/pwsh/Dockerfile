FROM mcr.microsoft.com/azure-powershell

copy run.ps1 .

SHELL ["pwsh", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

ENTRYPOINT [ "pwsh", "-File", "run.ps1"]

## Build: docker build -t n8-job-pwsh .