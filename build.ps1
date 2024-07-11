$ErrorActionPreference = "Stop"
$mainFolder = Resolve-Path (Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)

& dotnet tool restore
& dotnet paket restore
& dotnet build -c Release
