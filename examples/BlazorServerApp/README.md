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

The `"BrowserEnhancement"` feature is enabled when the user is using any of the allowed browsers, in this case, the Edge browser. If the `"BrowserEnhancement"` feature is on, the color of the top bar will be dark blue. Otherwise, it will be white.

The `"Beta"` feature uses the [`Targeting`](https://github.com/microsoft/FeatureManagement-Dotnet?tab=readme-ov-file#targeting) filter to evaluate when to activate. The `"Guests"` group, consisting of the unauthenticated users, will be excluded from the audience. Other users will have 50% chance to fall into the default rollout. When the `"Beta"` feature is enabled, the **BETA** content will be displayed.

## User Authentication

This app uses the [`AuthenticationStateProvider`](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-8.0#authenticationstateprovider-service) service to obtain the user authentication state information. When the application is loaded, the custom `RandomAuthenticationStateProvider` will generate a random user which has 2/3 chance to be authenticated.

## Targeting Context

In regular ASP.NET Core web app, the most common way to get targeting context is through the `HttpContextAccessor`. However, `IHttpContextAccessor` must be avoided with interactive rendering of Razor component in the Blazor server app because there isn't a valid `HttpContext` available.

[The recommended approach](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/server/interactive-server-side-rendering?view=aspnetcore-7.0#ihttpcontextaccessorhttpcontext-in-razor-components) to passing http context in Blazor apps is to copy the data into a scoped service. This app obtains the "User-Agent" information from the `HttpContext` and passes it to a scoped service called `UserAgentContextProvider`. `AuthenticationStateProvider` and `UserAgentContextProvider` services will be injected into the `TargetingContextAccessor`. Users will be assigned to `"Guests"` group if they are not authenticated and will be assigned to `"Edge"` group if they are using the Edge browser. 

To consume scoped services, the feature management services includes the feature manager, feature filters and `TargetingContextAccessor` are registered as scoped through `AddScopedFeatureManagement` API.
