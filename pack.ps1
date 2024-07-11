$ErrorActionPreference = "Stop"
$mainFolder = Resolve-Path (Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)

& "$mainFolder/build.ps1"
& dotnet pack --output "$mainFolder/Published"
