if not exist "%USERPROFILE%\.dotnet\tools\pwsh.exe" (
  dotnet tool install --global PowerShell --version 6.2.3
)

set PowerShellCore="%USERPROFILE%\.dotnet\tools\pwsh.exe"
