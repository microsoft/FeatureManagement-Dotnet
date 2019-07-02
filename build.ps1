<#
.Synopsis
This script builds all of the projects in this repo.

.Parameter BuildConfig
Indicates whether the build config should be set to Debug or Release. The default is Release.
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug','Release')]
    [string]$BuildConfig = "Release"
)

$ErrorActionPreference = "Stop"

$BuildRelativePath = "bin\BuildOutput"
$LogDirectory = "$PSScriptRoot\buildlogs"
$Solution     = "$PSScriptRoot\Microsoft.FeatureManagement.sln"

# Create the log directory.
if ((Test-Path -Path $LogDirectory) -ne $true) {
    New-Item -ItemType Directory -Path $LogDirectory | Write-Verbose
}

# Build
dotnet build -o "$BuildRelativePath" -c $BuildConfig "$Solution" | Tee-Object -FilePath "$LogDirectory\build.log"

exit $LASTEXITCODE
