# Tests.FeatureManagement.Telemetry.AzureMonitor

This project contains tests for the `Microsoft.FeatureManagement.Telemetry.AzureMonitor` package.

## Test Coverage

### AzureMonitorTelemetryTests

This test class verifies the integration between Feature Management and Azure Monitor telemetry through structured logging.

#### Tests Included

1. **LogsFeatureEvaluationWithAzureMonitor**
   - Verifies that feature evaluations are logged with the correct format
   - Ensures the "FeatureEvaluation" event name is present
   - Validates that logs are at Information level

2. **AzureMonitorHostedServiceStartsSuccessfully**
   - Tests that the hosted service starts and registers correctly
   - Verifies the application can start and stop without errors
   - Confirms feature management works after service startup

3. **LogsMultipleFeatureEvaluations**
   - Tests that multiple feature evaluations are all logged
   - Verifies the system can handle multiple concurrent feature checks
   - Ensures no logs are lost during high activity

4. **IntegrationWithAzureMonitorOpenTelemetry**
   - Tests integration with Azure Monitor OpenTelemetry patterns
   - Verifies logs contain properly formatted JSON properties
   - Validates the structured logging format for Azure Monitor ingestion

5. **LogContainsFeatureNameAndResult**
   - Verifies that feature names are included in log messages
   - Tests both enabled and disabled feature states
   - Ensures the serialized properties contain relevant information

## Testing Approach

The tests use a custom `TestLoggerProvider` to capture log messages without requiring actual Azure Monitor infrastructure. This allows for:

- Fast test execution
- No external dependencies
- Verification of log format and content
- Testing in CI/CD pipelines

## Integration with OpenTelemetry

The tests demonstrate that the package works seamlessly with:
- ASP.NET Core logging infrastructure
- Azure Monitor OpenTelemetry SDK (`Azure.Monitor.OpenTelemetry.AspNetCore`)
- Standard ILogger patterns

In production, you would configure Azure Monitor with:

```csharp
builder.Services.AddOpenTelemetry().UseAzureMonitor();
builder.Services.AddFeatureManagement().AddAzureMonitorTelemetry();
```

The feature evaluation events will automatically flow through the logging pipeline to Azure Monitor.

## Running Tests

```bash
dotnet test tests/Tests.FeatureManagement.Telemetry.AzureMonitor/Tests.FeatureManagement.Telemetry.AzureMonitor.csproj
```

Or run all tests in the solution:

```bash
dotnet test Microsoft.FeatureManagement.sln
```
