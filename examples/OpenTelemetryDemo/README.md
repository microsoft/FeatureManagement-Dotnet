# Evaluation Data to OpenTelemetry

This sample shows how to send feature flag evaluation data to an OpenTelemetry-compatible
backend using `Microsoft.FeatureManagement.Telemetry.OpenTelemetry`, exported to Azure Monitor
via the [Azure Monitor OpenTelemetry Distro](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore)
(`Azure.Monitor.OpenTelemetry.AspNetCore`). Evaluation data is emitted each time a feature or
variant is evaluated with telemetry enabled.

This package is published alongside `Microsoft.FeatureManagement.Telemetry.ApplicationInsights`
(see the [VariantAndTelemetryDemo](../VariantAndTelemetryDemo) sample) so that applications not
yet ready to move off the Application Insights SDK aren't forced to. New applications are
encouraged to use `Microsoft.FeatureManagement.Telemetry.OpenTelemetry`, the industry-standard,
vendor-neutral observability framework.

## Quickstart

To run this sample, follow these steps:

1. [Set up a new Application Insights resource in Azure](https://learn.microsoft.com/en-us/azure/azure-monitor/app/create-workspace-resource),
   and from `Overview` copy the `Connection String`.
2. Place the connection string in `appsettings.json` at `APPLICATIONINSIGHTS_CONNECTION_STRING`
   (or set it as an environment variable of the same name instead).
3. Set the project as the startup project (or `cd` into this directory).
4. Run the project (`dotnet run`) and browse to the app.
5. Head to the Application Insights resource in the Azure Portal to see the emitted telemetry
   (logs, traces, and metrics).

> A connection string is required for telemetry to be exported. Without one, the app still runs
> and requests succeed, but `UseAzureMonitor()` is skipped and nothing is exported.

## About the App

This app uses `Microsoft.FeatureManagement.Telemetry.OpenTelemetry` alongside the
[OpenTelemetry .NET SDK](https://github.com/open-telemetry/opentelemetry-dotnet) and the Azure
Monitor OpenTelemetry Distro to export logs, traces, and metrics. See `Program.cs` for how
tracing/logging/metrics and Azure Monitor are wired up via `UseAzureMonitor()`.

### Targeting Id

In order to connect evaluation events with other telemetry from the user, a targeting id needs
to be emitted. This sample uses the provided `TargetingHttpContextMiddleware`, which reads the
targeting context and adds `TargetingId` to both the `HttpContext` and the current `Activity`'s
baggage as a request comes in. `TargetingActivityProcessor` and `TargetingLogProcessor`
(automatically registered by `AddFeatureManagement().AddOpenTelemetry()`) then read that baggage
to enrich spans and logs respectively.

## Sample App Usage

Sample steps to try out the app:

1. Run the app. When the app is first started a user id will be generated and stored in an
   authentication cookie (see `RandomizeUser`).
2. When the page is loaded, the `ImageRating` feature is evaluated, which
   [defines three variants](./appsettings.json), emitting a `FeatureEvaluation` custom event and
   trace.
3. Select a rating for the loaded image and click vote. A `Vote` custom event and an
   `ImageRating` metric will be emitted.
4. Go to Checkout and click "Check Out", which emits a `checkout` custom event and a
   `checkoutAmount` metric.
5. Head to the Application Insights resource in the Azure Portal.
   1. Try going to Logs > New Query and run the query `customEvents`. This should show the
      custom events emitted, each carrying a `TargetingId` property.
   1. Try going to Metrics. Under Metric find Custom > `ImageRating` and `checkoutAmount`.
   1. From the Metrics window, out-of-the-box metrics like Page Views and Server Requests can be
      viewed thanks to the ASP.NET Core request instrumentation `UseAzureMonitor()` adds
      automatically.
