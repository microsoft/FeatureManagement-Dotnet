# Blazor Server App

This sample shows the recommended way to use the Feature Management library in a Blazor Server App.

## Quickstart

To run this sample, follow these steps:

1. Set the project as the startup project.
2. Run the project.
3. Refresh the page until the BETA content occurs.

## About the App

This app demonstrates how to use the Feature Management library in Blazor apps.

This app uses two feature flags: "BrowserEnhancement" and "Beta".

``` json
"FeatureManagement": {
    "BrowserEnhancement": {
        "EnabledFor": [
            {
                "Name": "Browser",
                "Parameters": {
                    "AllowedBrowsers": [ "Edge" ]
                }
            }
        ]
    },
    "Beta": {
        "EnabledFor": [
            {
                "Name": "Targeting",
                "Parameters": {
                    "Audience": {
                        "DefaultRolloutPercentage": 50,
                        "Exclusion": {
                            "Groups": [
                                "Guests"
                            ]
                        }
                    }
                }
            }
        ]
    }
}
```

The `"BrowserEnhancement"` feature is enabled when the user is using the allowed browsers, in this example, the Edge browser. If the `"BrowserEnhancement"` feature is on, the color of the top bar will be dark blue. Otherwise, it will be white.

The `"Beta"` feature uses the [`Targeting`](https://github.com/microsoft/FeatureManagement-Dotnet?tab=readme-ov-file#targeting) filter to evaluate when to activate. The `"Guests"` group, consisting of the unauthenticated users, will be excluded from the audience. Other users will have 50% chance to fall into the default rollout. When the `"Beta"` feature is enabled, the **BETA** content will be displayed.

## HttpContext

In regular ASP.NET Core web app, the most common way to get targeting context is through the `HttpContextAccessor`. However, `IHttpContextAccessor` must be avoided with interactive rendering of Razor component in the Blazor server app because there isn't a valid `HttpContext` available. More details can be found [here](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/server/interactive-server-side-rendering?view=aspnetcore-7.0#ihttpcontextaccessorhttpcontext-in-razor-components).

[The recommended approach](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/server/interactive-server-side-rendering?view=aspnetcore-7.0#ihttpcontextaccessorhttpcontext-in-razor-components) to pass the http context in Blazor apps is to copy the data into a scoped service. This app obtains the `"User-Agent"` information from the `HttpContext` and passes it to a scoped service called `UserAgentContext`. `UserAgentContext` will be consumed by the `BrowserFilter` through dependency injection.

## User Authentication State

This app uses [cookie authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-6.0). When the app is loaded, it has 2/3 chance to be an authenticated user. The details can be found in the [`_Host.cshtml`](./examples/BlazorServerApp/Pages/_Host.cshtml).

Rather than `HttpContext`, the [`AuthenticationStateProvider`](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-8.0#authenticationstateprovider-service) service is used to obtain the user authentication state information for setting targeting context. The details can be found in the [`MyTargetingContextAccessor`](./examples/BlazorServerApp/MyTargetingContextAccessor.cs).

## Service Registration
Blazor applications like this one typically pull ambient contextual data from scoped services. For example, the `UserAgentContext`, `AuthenticationStateProvider` and `ITargetingContextAccessor` are all scoped services. This pattern *breaks* if the feature management services are added as singleton, which is typical in non-blazor web apps.

In Blazor, *avoid* the following
``` C#
services.AddFeatureManagment()
```

And instead use
``` C#
services.AddScopedFeatureManagement()
```

This call can be seen in the [example](./examples/BlazorServerApp/Program.cs).

