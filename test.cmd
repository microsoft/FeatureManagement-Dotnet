cd /D "%~dp0"

dotnet test tests\Tests.FeatureManagement\Tests.FeatureManagement.csproj --logger trx ||  exit /b 1
