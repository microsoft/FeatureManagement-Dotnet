$ErrorActionPreference = "Stop"

$dotnet = & "$PSScriptRoot/build/resolve-dotnet.ps1"

& $dotnet test "$PSScriptRoot\tests\Tests.FeatureManagement\Tests.FeatureManagement.csproj" --logger trx

exit $LASTEXITCODE