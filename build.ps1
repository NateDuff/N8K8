dotnet publish .\web\N8.Web.csproj -c Release -p:PublishProfile=DefaultContainer --no-restore

dotnet publish .\api\N8.API.csproj -c Release -p:PublishProfile=DefaultContainer --no-restore

dotnet publish .\worker\N8.Worker.csproj -c Release -p:PublishProfile=DefaultContainer --no-restore
