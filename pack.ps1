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

$targetProjects = @(

    "Microsoft.FeatureManagement",
    "Microsoft.FeatureManagement.AspNetCore"
)

# Create the log directory.
if ((Test-Path -Path $LogDirectory) -ne $true) {
    New-Item -ItemType Directory -Path $LogDirectory | Write-Verbose
}

foreach ($project in $targetProjects)
{
    $projectPath = "$PSScriptRoot\src\$project\$project.csproj"
    $outputPath = "$PSScriptRoot\src\$project\$PublishRelativePath"

    #
    # The build system expects pre-built binaries to be in the folder pointed to by 'OutDir'.
    dotnet pack -o "$outputPath" /p:OutDir=$PrebuiltBinariesDir "$projectPath" --no-build | Tee-Object -FilePath "$LogDirectory\build.log"
}

exit $LASTEXITCODE
