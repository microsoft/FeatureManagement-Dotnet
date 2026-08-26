$ErrorActionPreference = "Stop"

$dotnet = & "$PSScriptRoot/build/resolve-dotnet.ps1"

$testProjects = @(
    "Tests.FeatureManagement",
    "Tests.FeatureManagement.AspNetCore",
    "Tests.FeatureManagement.Telemetry.OpenTelemetry"
)

foreach ($project in $testProjects)
{
    & $dotnet test "$PSScriptRoot\tests\$project\$project.csproj" --logger trx

    if ($LASTEXITCODE -ne 0)
    {
        exit $LASTEXITCODE
    }
}

exit $LASTEXITCODE