# Evaluation Data to Azure Monitor# Evaluation Data to Application Insights



This sample shows how to send evaluation data to Azure Monitor using OpenTelemetry. Evaluation data occurs each time a feature result is determined by the Feature Manager.This sample shows how to send evaluation data to Application Insights. Evaluation data occurs each time a feature result is determined by the Feature Manager.



## Quickstart## Quickstart



To run this sample, follow these steps:To run this sample, follow these steps:



1. Set the project as the startup project.1. Set the project as the startup project.

2. Run the project.2. Run the project.

3. Telemetry will be collected using OpenTelemetry and sent to Azure Monitor.3. Check the Output tab for `Application Insights Telemetry`.



## Connecting to Azure MonitorExample Output:



To flow these events to Azure Monitor, [setup a new Application Insights resource in Azure](https://learn.microsoft.com/en-us/azure/azure-monitor/app/create-workspace-resource). Once setup, from `Overview` copy the `Connection String` and set it as an environment variable:```

Application Insights Telemetry: {"name":"AppEvents","time":"2023-11-07T19:14:54.3549353Z","tags":{"ai.application.ver":"1.0.0.0"},"data":{"baseType":"EventData","baseData":{"ver":2,"name":"Vote","properties":{"AspNetCoreEnvironment":"Development","DeveloperMode":"true"},"measurements":{"ImageRating":3}}}}

```bashApplication Insights Telemetry: {"name":"AppEvents","time":"2023-11-07T19:14:54.4143414Z","tags":{"ai.application.ver":"1.0.0.0"},"data":{"baseType":"EventData","baseData":{"ver":2,"name":"FeatureEvaluation","properties":{"Label":"A Label","Etag":"An etag","AspNetCoreEnvironment":"Development","DeveloperMode":"true","Variant":"WithColor","FeatureName":"ImageRating","Tags.A":"Tag A value","IsEnabled":"True"}}}}

set APPLICATIONINSIGHTS_CONNECTION_STRING=<your-connection-string>```

```

These logs show what would be emitted to a connected Application Insights resource, even if one is not yet connected.

Or add it to `appsettings.json`:

## Connecting to Application Insights

```json

{To flow these events to Application Insights, [setup a new Application Insights resource in Azure](https://learn.microsoft.com/en-us/azure/azure-monitor/app/create-workspace-resource). Once setup, from `Overview` copy the `Connection String` and place it in `appsettings.json` at ApplicationInsights > ConnectionString. After restarting the app, events should now flow to Application Insights. This [document](https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core?tabs=netcorenew%2Cnetcore6#enable-application-insights-server-side-telemetry-no-visual-studio) provides more details on connecting a .NET application to Application Insights.

  "APPLICATIONINSIGHTS_CONNECTION_STRING": "<your-connection-string>"

}## About the App

```

This app uses [Application Insights for ASP.NET Core](https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core?tabs=netcorenew%2Cnetcore6). This means there is an App Insights SDK in the C# code and a separate App Insights SDK the Javascript. Lets cover what they're doing:

After restarting the app, events should now flow to Azure Monitor. For more details, see [Use Azure Monitor OpenTelemetry for .NET applications](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore).

### Javascript App Insights SDK

## About the App

See [Enable cliend-side telemetry for web applications](https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core?tabs=netcorenew%2Cnetcore6#enable-client-side-telemetry-for-web-applications)

This app uses [Azure Monitor OpenTelemetry for ASP.NET Core](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore) which is built on the OpenTelemetry standard. This provides automatic instrumentation for common scenarios and allows custom telemetry using the OpenTelemetry API.

For ASP.NET, this is added to _ViewImports.cshtml

### Key Differences from Application Insights SDK```html

@inject Microsoft.ApplicationInsights.AspNetCore.JavaScriptSnippet JavaScriptSnippet

Instead of using the Application Insights SDK directly, this example uses:```

and then in the _Layout.cshtml `<head>`:

- **OpenTelemetry API**: For custom metrics and events```html

  - `IMeterFactory` and `Meter` for custom metrics@Html.Raw(JavaScriptSnippet.FullScript)

  - `Activity` and `ActivityEvent` for custom events and spans```

- **Azure Monitor OpenTelemetry Distro**: Configured via `builder.Services.AddOpenTelemetry().UseAzureMonitor()`

- **Feature Management Integration**: Uses `.AddAzureMonitorTelemetry()` to emit feature evaluation eventsThe Javascript SDK will collect telemetry from the browser and send it to the Application Insights resource. Some examples are Page Views and Browser Timings. The Javascript SDK also automatically generates useful cookies like:

1. `ai_user` - a unique user id

### Custom Telemetry Examples1. `ai_session` - a unique session id



**Custom Metrics (Index.cshtml.cs)**:These cookies are used to correlate telemetry from the browser with telemetry from the server. The ASP.NET Application Insights SDK will detect these cookies and append them to telemetry it sends.

```csharp

var imageRatingHistogram = _meter.CreateHistogram<long>("ImageRating");*The Javascript SDK is not required, but is useful for collecting browser telemetry and generating these cookies out of the box.*

imageRatingHistogram.Record(rating);

```### Targeting Id



**Custom Events (Checkout.cshtml.cs)**:In order to connect evaluation events with other metrics from the user, a targeting id needs to be emitted. This can be done multiple ways, but the recommended way is to define a telemetry initializer. This initializer allows the app to modify all telemetry going to Application Insights before it's sent.

```csharp

Activity.Current?.AddEvent(new ActivityEvent("checkout", This example uses the provided `TargetingHttpContextMiddleware` and `TargetingTelemetryInitializer`. The middleware adds `TargetingId` (using the targeting context accessor) to the HTTP Context as a request comes in. The initializer checks for the `TargetingId` on the HTTP Context, and if it exists, adds `TargetingId` to all outgoing Application Insights Telemetry.

    tags: new ActivityTagsCollection { { "success", "yes" } }));

```## Sample App Usage



For more information on sending custom telemetry with OpenTelemetry, see [Add and modify Azure Monitor OpenTelemetry](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-add-modify?tabs=aspnetcore).Sample steps to try out the app:



## Sample App Usage1. Run the app. When the app is first started a User Id and Session Id will be generated. The username cookie will be set to a random integer, and the ai_user and ai_session cookies will be expired.

1. When the page is loaded, the "ImageRating" feature is evaluated which [defines three variants](./appsettings.json). Events can be seen in the Output window. (There may be a small delay as events are batched)

Sample steps to try out the app:1. Select a rating for the loaded image and click vote. A "Vote" event will be emitted.

1. Go to Checkout and click "Check Out", which emits a custom event and a custom metric. This event and metric will be shown in the logs as well.

1. Run the app. When the app is first started a User Id will be generated and the username cookie will be set to a random integer.1. If connected to Application Insights, head to the resource in the portal. Events and metrics will be there as well.

2. When the page is loaded, the "ImageRating" feature is evaluated which [defines three variants](./appsettings.json). OpenTelemetry will track this evaluation.    1. Try going to Logs > New Query and run the query "customEvents". This should show the custom events emitted.

3. Select a rating for the loaded image and click vote. An "ImageRating" metric will be recorded.	1. Try going to Metrics. Under Metric find Custom > checkoutAmount. Change the time range to a small period of time that encompasses your events for a clearer graph.

4. Go to Checkout and click "Check Out", which emits a custom event and a custom metric using OpenTelemetry.	1. From the Metrics window, out-of-the-box metrics like Page Views and Server Requests can be viewed.
5. If connected to Azure Monitor, head to the Application Insights resource in the Azure portal:
   - Go to **Logs** > **New Query** and run queries like:
     - `customEvents` - to see custom events
     - `customMetrics` - to see custom metrics
   - Go to **Metrics** to visualize metrics like `ImageRating` and `checkoutAmount`
   - View **Dependencies** and **Requests** for automatically collected telemetry
