# Evaluation Data to OpenTelemetry

This sample shows how to send feature flag evaluation data to an OpenTelemetry-compatible
backend using `Microsoft.FeatureManagement.Telemetry.OpenTelemetry`. Evaluation data is
emitted each time a feature or variant is evaluated with telemetry enabled.

This package is published alongside `Microsoft.FeatureManagement.Telemetry.ApplicationInsights`
(see the [VariantAndTelemetryDemo](../VariantAndTelemetryDemo) sample), not as its replacement,
so that applications not yet ready to move off Application Insights sink aren't forced to. However,
an application should choose only one of the two sinks — wiring up both would emit duplicate
`FeatureEvaluation` events for the same evaluation.

## Quickstart

To run this sample, follow these steps:

1. Set the project as the startup project (or `cd` into this directory).
2. Run the project (`dotnet run`).
3. Observe the console output for each simulated user.

Example output:

```
info: Microsoft.FeatureManagement.Telemetry.OpenTelemetry.OpenTelemetryEventPublisher[0]
      FeatureEvaluation
LogRecord.Attributes (Key:Value):
    microsoft.custom_event.name: FeatureEvaluation
    FeatureName: ImageRating
    Enabled: true
    VariantAssignmentReason: Percentile
    TargetingId: Alice
    Variant: BlackAndWhite
    ...

User 'Alice' was assigned variant 'BlackAndWhite'.
```

These logs show what would be emitted to a connected OpenTelemetry backend, even if one is not
yet connected (a console exporter is used here for demonstration).

## About the App

This app is a .NET Generic Host console application that evaluates a variant feature,
`ImageRating`, for a few simulated users and demonstrates two independent pieces of
OpenTelemetry wiring:

### 1. The `FeatureEvaluation` log-based custom event

```csharp
services.AddLogging(logging =>
{
    logging.AddOpenTelemetry(options =>
    {
        options.AddConsoleExporter();
        // options.AddAzureMonitorLogExporter(o => o.ConnectionString = "<connection-string>");
    });
});

services.AddFeatureManagement()
    .AddOpenTelemetry();
```

`AddOpenTelemetry()` on the feature management builder registers
`OpenTelemetryEventPublisher`, which listens to the `Activity`/`ActivityEvent` already emitted
by the core `Microsoft.FeatureManagement` library and turns it into an OpenTelemetry
`LogRecord` with the `microsoft.custom_event.name` attribute set to `FeatureEvaluation`. This
works independently of any `TracerProvider`; it only requires an `ILogger` (wired up here via
`AddLogging`/`AddOpenTelemetry`).

To flow this event to Azure Monitor, set `ApplicationInsights:ConnectionString` in
`appsettings.json` (or an environment variable of the same name) to a real
[Application Insights connection string](https://learn.microsoft.com/en-us/azure/azure-monitor/app/create-workspace-resource),
which enables the commented-out `AddAzureMonitorLogExporter` call above. The event will then be
classified into the `customEvents` table.

### 2. Enriching traces/spans with `TargetingId`

```csharp
services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("Microsoft.FeatureManagement");
        tracing.AddConsoleExporter();
    });
```

This is only needed if the app also wants to export the raw `Activity`/span data produced by
feature evaluation (separate from the log-based custom event above). Once wired up,
`TargetingActivityProcessor` (registered by `AddOpenTelemetry()` on the feature management
builder) is added to the `TracerProvider`:

```csharp
host.Services.GetRequiredService<TracerProvider>()
    .AddProcessor(host.Services.GetRequiredService<TargetingActivityProcessor>());
```

`TargetingActivityProcessor` stamps a `TargetingId` tag onto every processed `Activity`/span
based on its baggage, the span equivalent of `TargetingTelemetryInitializer` in the
Application Insights package.

## Sample App Usage

The app evaluates the `ImageRating` feature (configured with two variants split 50/50 by
percentile, see [appsettings.json](./appsettings.json)) for three simulated users
(`Alice`, `Bob`, `Carol`), each with a different `TargetingId`. For each user, both the
console-exported `Activity` and the resulting `FeatureEvaluation` `LogRecord` are printed.
