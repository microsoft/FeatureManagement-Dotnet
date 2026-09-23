# Installs .NET 8 and .NET 10 for CI/CD environment
# see: https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script#examples

$ErrorActionPreference = "Stop"

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12;

$installScript = [scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))

$channels = @("8.0", "10.0")

foreach ($channel in $channels)
{
    & $installScript -Channel $channel
}

# Fail fast with a clear error instead of letting a silent install failure surface later as a
# confusing "You must install or update .NET to run this application" error during test/build.
$dotnet = & "$PSScriptRoot/resolve-dotnet.ps1"
$installedSdks = & $dotnet --list-sdks

foreach ($channel in $channels)
{
    if (-not ($installedSdks | Select-String -Pattern "^$([regex]::Escape($channel))\."))
    {
        throw "Verification failed: .NET SDK for channel $channel was not found after installation. Installed SDKs:`n$installedSdks"
    }
}
