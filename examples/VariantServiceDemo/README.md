# Variant Service Demo

This sample shows how to use variant feature flag in conjunction with dependency injection to surface different implementations of a service for different users.

## Quickstart

To run this sample, follow these steps:

1. Set the project as the startup project.
2. Run the project.
3. Enter two numbers and click the *Calculate* button.
4. Refresh the page and a new visitor with a random ID will be generated.
5. Repeat the step 3 and 4.

## About the App

This app demonstrates how to use the `IVariantServiceProvider<ICalculator` to retrieve different implementation of interface `ICalculator` from the dependency injection container. 

``` C#
private readonly IVariantServiceProvider<ICalculator> _calculatorProvider;

public IndexModel(IVariantServiceProvider<ICalculator> calculatorProvider)
{
    _calculatorProvider = calculatorProvider ?? throw new ArgumentNullException(nameof(calculatorProvider));
}

public async Task<JsonResult> OnGetCalculate(double a, double b)
{
    ICalculator calculator = await _calculatorProvider.GetServiceAsync(HttpContext.RequestAborted);

    double result = await calculator.AddAsync(a, b);

    return new JsonResult(result);
}
```

## Variant Service Registration

The `IVariantServiceProvider<T>` is made available to the application by calling `IFeatureManagementBuilder.WithVariantService<T>(string featureName)`. 

``` C#
builder.Services.AddFeatureManagement()
    .WithVariantService<ICalculator>("Calculator");
```

The call above makes ``IVariantServiceProvider<ICalculator>`` available in the service collection and bind it with a variant feature flag called "Calculator".

``` json
{
  "id": "Calculator",
  "enabled": true,
  "telemetry": {
    "enabled": true
  },
  "variants": [
    {
      "name": "DefaultCalculator"
    },
    {
      "name": "RemoteCalculator"
    }
  ],
  "allocation": {
    "percentile": [
      {
        "variant": "DefaultCalculator",
        "from": 0,
        "to": 50
      },
      {
        "variant": "RemoteCalculator",
        "from": 51,
        "to": 100
      }
    ]
  }
}
```

Implementations of `ICalculator` are added separately.

``` C#
builder.Services.AddSingleton<ICalculator, DefaultCalculator>();

builder.Services.AddSingleton<ICalculator, RemoteCalculator>();
```

The `DefaultCalculator` will do the calculation on the server locally. The `RemoteCalculator` will call the [newton API](https://github.com/aunyks/newton-api) to get the result of the calculation.

## A/B Testing

A/B testing allows to make careful changes to their user experiences while collecting data on the impact it makes.

This sample will use Application Insights to collect telemetry for A/B testing. Application Insights will collect request telemetry for each http request automatically. The request telemetry includes the *duration* dimension which measures the elapsed time of the http request. This can be an indicator of the impact made by the variant service.

### Connecting to Application Insights

[Setup a new Application Insights resource in Azure](https://learn.microsoft.com/en-us/azure/azure-monitor/app/create-workspace-resource). Once setup, from `Overview` copy the `Connection String` and place it in `appsettings.json` at ApplicationInsights > ConnectionString. After restarting the app, telemetry should now flow to Application Insights. This [document](https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core?tabs=netcorenew%2Cnetcore6#enable-application-insights-server-side-telemetry-no-visual-studio) provides more details on connecting a .NET application to Application Insights.

### Analyze the collected telemetry

Go to the Application Insights store in your Azure portal. Under Monitoring > Logs, run the following query.

```
customEvents
| where name == "FeatureEvaluation"
| project TargetingId = tostring(customDimensions.TargetingId), Variant = tostring(customDimensions.Variant)
| join (
    requests
    | where url matches regex @"https://localhost:\d+/Index\?handler=Calculate"
    | project TargetingId = tostring(customDimensions.TargetingId), Duration = todouble(duration)
  ) on TargetingId
| project TargetingId, Variant, Duration
| summarize Duration = avg(Duration) by Variant
```

You will see the average call durations of two variants.