call %~dp0build\ChoosePowerShell.cmd

IF %ERRORLEVEL% NEQ 0 (

    exit /B 1
)

%PowerShell% "%~dp0pack.ps1" %*