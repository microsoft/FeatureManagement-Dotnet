# Microsoft.FeatureManagement.Telemetry.AzureMonitor

This package provides a solution for sending feature flag evaluation events produced by the Microsoft.FeatureManagement library to Azure Monitor using structured logging.

## Overview

The `Microsoft.FeatureManagement.Telemetry.AzureMonitor` package uses an `ActivityListener` to capture feature flag evaluation events and emits them through structured logging, making them available for Azure Monitor ingestion.

## Usage

To add Azure Monitor telemetry support to your feature management setup:

```csharp
services.AddFeatureManagement()
    .AddAzureMonitorTelemetry();
```

## How It Works

The package uses an `ActivityListener` to listen to feature evaluation events from the Microsoft.FeatureManagement library. When a feature flag is evaluated, the event is logged using a structured logging extension method:

```csharp
_logger.LogFeatureEvaluation("FeatureEvaluation", properties);
```

This extension method formats the event data properly for Azure Monitor ingestion using the `LoggerMessage.Define` pattern for high-performance logging. The properties dictionary is serialized to JSON and included in the log message.

## Key Components

- **AzureMonitorEventPublisher**: Listens to Activity events from feature management and logs them using the logger extension
- **LoggerExtensions**: Provides optimized logging methods for feature evaluation events
- **AzureMonitorHostedService**: Manages the lifecycle of the event publisher
- **FeatureManagementBuilderExtensions**: Provides extension methods to register the telemetry components

## Differences from ApplicationInsights Package

Unlike the `Microsoft.FeatureManagement.Telemetry.ApplicationInsights` package which directly uses `TelemetryClient`, this package:
- Uses structured logging instead of direct TelemetryClient calls
- Has no dependency on the Application Insights SDK
- Is more flexible and can work with any logging provider configured in your application
- Follows the standard .NET logging patterns

## Requirements

- Microsoft.Extensions.Logging.Abstractions 8.0.2 or higher
- Microsoft.Extensions.Hosting.Abstractions 8.0.0 or higher
- Microsoft.FeatureManagement

## License

This project is licensed under the MIT License.
