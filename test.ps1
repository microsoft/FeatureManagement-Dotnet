$ErrorActionPreference = "Stop"

$dotnet = & "$PSScriptRoot/build/resolve-dotnet.ps1"

& $dotnet test "$PSScriptRoot\tests\Tests.FeatureManagement\Tests.FeatureManagement.csproj" --logger trx

if ($LASTEXITCODE -ne 0)
{
	exit $LASTEXITCODE
}

& $dotnet test "$PSScriptRoot\tests\Tests.FeatureManagement.AspNetCore\Tests.FeatureManagement.AspNetCore.csproj" --logger trx

exit $LASTEXITCODE