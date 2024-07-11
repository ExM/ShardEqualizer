param (
    [string]$apikey = ""
)

$ErrorActionPreference = "Stop"
$mainFolder = Resolve-Path (Split-Path -Path $MyInvocation.MyCommand.Definition -Parent)
if ($apikey -Eq "")
{
	Get-ChildItem -Path "$mainFolder\Published\*.nupkg" | foreach { & dotnet nuget push $_ -s http://nexus-repo.sc.local/repository/nuget/ }
}
else
{
	Get-ChildItem -Path "$mainFolder\Published\*.nupkg" | foreach { & dotnet nuget push $_ -s http://nexus-repo.sc.local/repository/nuget/ -k $apikey }
}

if ($lastexitcode -ne 0)
{
	Write-Host "Error while pushing" -ForegroundColor Red
	Exit 1
}

