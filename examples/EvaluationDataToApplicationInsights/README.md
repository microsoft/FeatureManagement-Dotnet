# Evaluation Data to Application Insights

This sample shows how to send evaluation data to Application Insights. Evaluation data occurs each time a feature result is determined by the Feature Manager.

## Quickstart

To run this sample, follow these steps:

1. Set the project as the startup project.
2. Run the project.
3. Check the Output tab for `Application Insights Telemetry`.

Example Output:

```
Application Insights Telemetry (unconfigured): {"name":"AppEvents","time":"2023-10-16T21:38:54.1357063Z","tags":{"ai.cloud.roleInstance":"abc-123","ai.operation.id":"5a1ad3252d6bac6b05c026d76157f77e","ai.operation.parentId":"a0f0ea8cb5a45b8d","ai.internal.sdkVersion":"dotnetc:2.21.0-429"},"data":{"baseType":"EventData","baseData":{"ver":2,"name":"FeatureEvaluation","properties":{"DeveloperMode":"true","IsEnabled":"True","FeatureName":"FeatureA"}}}}
Application Insights Telemetry (unconfigured): {"name":"AppEvents","time":"2023-10-16T21:38:54.1869228Z","tags":{"ai.cloud.roleInstance":"abc-123","ai.operation.id":"5a1ad3252d6bac6b05c026d76157f77e","ai.operation.parentId":"a0f0ea8cb5a45b8d","ai.internal.sdkVersion":"dotnetc:2.21.0-429"},"data":{"baseType":"EventData","baseData":{"ver":2,"name":"FeatureEvaluation","properties":{"DeveloperMode":"true","IsEnabled":"False","FeatureName":"FeatureB"}}}}
```

These logs show what would be emitted to a connected Application Insights resource, even if one is not yet connected.

## Connecting to Application Insights

To flow these events to Application Insights, [setup a new Application Insights resource in Azure](https://learn.microsoft.com/en-us/azure/azure-monitor/app/create-workspace-resource). Once setup, from `Overview` copy the `Connection String` and place it in `appsettings.json` at ApplicationInsights > ConnectionString. After restarting the app, events should now flow to Application Insights.

## About the App
This app uses [Application Insights for ASP.NET Core](https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core?tabs=netcorenew%2Cnetcore6). This means there is an App Insights SDK in the C# code and a separate App Insights SDK the Javascript. Lets cover what they're doing:

### Javascript App Insights SDK
See [Enable cliend-side telemetry for web applications](https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core?tabs=netcorenew%2Cnetcore6#enable-client-side-telemetry-for-web-applications)

For ASP.NET, this is added to _ViewImports.cshtml
```html
@inject Microsoft.ApplicationInsights.AspNetCore.JavaScriptSnippet JavaScriptSnippet
```
and then in the _Layout.cshtml `<head>`:
```html
@Html.Raw(JavaScriptSnippet.FullScript)
</head>
```

The Javascript SDK will collect telemetry from the browser and send it to the Application Insights resource. Some examples are Page Views and Browser Timings. The Javascript SDK also automatically generates useful cookies like:
1. `ai_user` - a unique user id
1. `ai_session` - a unique session id

These cookies are used to correlate telemetry from the browser with telemetry from the server. The ASP.NET Application Insights SDK will detect these cookies and append them to telemetry it sends.

*The Javascript SDK is not required, but is useful for collecting browser telemetry and generating these cookies out of the box.*

### Authenticated User ID
In order to connect metrics for the user between multiple services, a Authenticated User Id needs to be emitted. When a user clicks "Randomize User", a login is simulated by setting a "username" cookie to a random integer. Additionally, the "ai_user" and "ai_session" cookies are expired, to simulate a new browser.

To include the authenticated user id on metrics emitted from the Javascript SDK, this app adds the following to _Layout.cshtml:
```html
appInsights.setAuthenticatedUserContext(getCookie("username"));
```

To include it on metrics emitted from the ASP.NET SDK, this app uses a TelemetryInitializer named `MyTelemetryInitializer`:
```csharp
builder.Services.AddSingleton<ITelemetryInitializer, MyTelemetryInitializer>();
```

The initializer sets the Authenticated User on the context object for all telemetry emitted from the server:
```csharp
telemetry.Context.User.AuthenticatedUserId = username;
```

## Sample App Usage
Sample steps to try out the app:

1. Run the app. When the app is first started, it is in the un-logged in state. User Id and Session Id will still be generated, but Authenticated User Id will be null. 
1. Click Randomize User to simulate a login on a new browser. The username cookie will be set to a random integer, and the ai_user and ai_session cookies will be expired.
1. When the page is loaded, feature flags are evaluated for FeatureA and FeatureB. Events can be seen in the Output window. (There may be a small delay as events are batched)
1. Click around the app and watch the logs. More events will be emitted.
1. Go to Checkout and click "Check Out", which emits a custom event and a custom metric. This event and metric will be shown in the logs as well.
1. If connected to Application Insights, head to the resource in the portal. Events and metrics will be there as well. 	
    1. Try going to Logs > New Query and run the query "customEvents". This should show the custom events emitted.
	1. Try going to Metrics. Under Metric find Custom > checkoutAmount. Change the time range to a small period of time that encompasses your events for a clearer graph.
	1. From the Metrics window, out-of-the-box metrics like Page Views and Server Requests can be viewed.