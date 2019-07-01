<#
.Synopsis
This script creates NuGet packages from all of the projects in this repo. 

Note: build.cmd should be run before running this script.

#>

[CmdletBinding()]
param(
)

$ErrorActionPreference = "Stop"

$PrebuiltBinariesDir = "bin\BuildOutput"
$PublishRelativePath = "bin\PackageOutput"
$LogDirectory = "$PSScriptRoot\buildlogs"
$Solution     = "$PSScriptRoot\Microsoft.FeatureManagement.sln"

# Create the log directory.
if ((Test-Path -Path $LogDirectory) -ne $true) {
    New-Item -ItemType Directory -Path $LogDirectory | Write-Verbose
}

#
# The build system expects pre-built binaries to be in the folder pointed to by 'OutDir'.
dotnet pack -o "$PublishRelativePath" /p:OutDir=$PrebuiltBinariesDir "$Solution" --no-build | Tee-Object -FilePath "$LogDirectory\build.log"

exit $LASTEXITCODE
