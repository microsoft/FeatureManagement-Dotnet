<#
.Synopsis
This script creates NuGet packages from all of the projects in this repo. 

Note: build.cmd should be run before running this script.

#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug','Release')]
    [string]$BuildConfig = "Release"
)

$ErrorActionPreference = "Stop"

$PublishRelativePath = "bin\PackageOutput"
$LogDirectory = "$PSScriptRoot\buildlogs"

$targetProjects = @(

    "Microsoft.FeatureManagement",
    "Microsoft.FeatureManagement.AspNetCore",
    "Microsoft.FeatureManagement.Telemetry.ApplicationInsights"
)

# Create the log directory.
if ((Test-Path -Path $LogDirectory) -ne $true) {
    New-Item -ItemType Directory -Path $LogDirectory | Write-Verbose
}

$dotnet = & "$PSScriptRoot/build/resolve-dotnet.ps1"

foreach ($project in $targetProjects)
{
    $projectPath = "$PSScriptRoot\src\$project\$project.csproj"
    $outputPath = "$PSScriptRoot\src\$project\$PublishRelativePath"

    & $dotnet pack -c $BuildConfig -o "$outputPath" "$projectPath" --no-build | Tee-Object -FilePath "$LogDirectory\build.log"
}

exit $LASTEXITCODE
