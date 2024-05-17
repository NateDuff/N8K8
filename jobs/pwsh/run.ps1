Write-Output "Hello from PowerShell!"

Get-Module Az* -ListAvailable | Select-Object Name,Version,Path
