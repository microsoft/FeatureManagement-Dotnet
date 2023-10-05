# Installs .NET 6 and .NET 7 for CI/CD environment
# see: https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script#examples

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12;

&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) -Channel 6.0

&([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) -Channel 7.0