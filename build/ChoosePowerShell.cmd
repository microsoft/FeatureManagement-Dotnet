:: where.exe does not exist in windows container, application specific test must be used to check for existence

PowerShell -Command Write-Host "a"

IF %ERRORLEVEL% == 0 (

    set PowerShell=PowerShell

    exit /B 0
)

pwsh -Command Write-Host "a"

IF %ERRORLEVEL% == 0 (

    set PowerShell=pwsh

    exit /B 0
)

echo Could not find a suitable PowerShell executable.

EXIT /B 1
