# Resolves dotnet execution path
# Locations considered include dotnet install script default location and somewhere on path
$CI_CD_INSTALL_PATH = "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe"

if (Test-Path $CI_CD_INSTALL_PATH)
{
    $CI_CD_INSTALL_PATH

    return
}

$dotnet = Get-Command dotnet.exe -ErrorAction Stop

$dotnet.Source