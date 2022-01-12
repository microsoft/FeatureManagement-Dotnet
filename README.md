# .NET Feature Management

The Microsoft.FeatureManagement library enables developers to use feature flags and dynamic features inside of their applications. Feature flags can be used to turn features on or off dynamically. Developers can use feature flags in simple use cases like conditional statements to more advanced scenarios like conditionally adding routes or MVC filters. Dynamic features can be used to select different variants of a feature's configuration. This enables the possibility of using one version of a feature for one set of users, and another version of the feature for the remaining users.

Here are some of the benefits of using this library:

* A common convention for feature management
* Low barrier-to-entry
  * Built on `IConfiguration`
  * Supports JSON file feature flag setup
* Feature Flag lifetime management
  * Configuration values can change in real-time, feature flags can be consistent across the entire request
* Simple to Complex Scenarios Covered
  * Toggle on/off features through declarative configuration file
  * Use different variants of a feature in different circumstances
* API extensions for ASP.NET Core and MVC framework
  * Routing
  * Filters
  * Action Attributes

**API Reference**: https://go.microsoft.com/fwlink/?linkid=2091700

### Feature Flags
Feature flags can either be on or off. They are composed of two parts, a name and a list of feature-filters that are used to turn the feature on.

### Feature Filters
Feature filters define a scenario for when a feature flag should be enabled. When a feature flag is evaluated for whether it is on or off, its list of feature-filters are traversed until one of the filters decides the feature flag should be enabled. At this point the feature flag is considered enabled and traversal through the feature filters stops. If no feature filter indicates that the feature flag should be enabled, then it will be considered disabled.

As an example, a Microsoft Edge browser feature filter could be designed. This feature filter would activate any features it is attached to as long as an HTTP request is coming from Microsoft Edge.

### Dynamic Features
Dynamic features can have values who's type range from object, to string, to integer and so on. Additionally, dynamic features can have an unlimited amount of values. A developer is free to choose what type should be returned when the value of a dynamic feature is requested. They are also free to choose how many options, known as variants, are available to select from.

### Feature Variants
Feature variants are the different versions of a feature that could be returned when the value of a dynamic feature is requested. Beyond the value of the feature, a feature variant contains information describing under what circumstances it should be returned over other available variants.

### Feature Variant Assigners
A feature variant assigner is a component that uses contextual information within an application to decide which feature variant should be chosen when a variant of a dynamic feature is requested.

## Registration

The .NET Core configuration system is used to determine the state of feature flags. The foundation of this system is `IConfiguration`. Any provider for IConfiguration can be used as the feature state provider for the feature flag library. This enables scenarios ranging from appsettings.json to Azure App Configuration and more.

### Feature Flag Declaration

The feature management library supports appsettings.json as a feature flag source since it is a provider for .NET Core's IConfiguration system. Below we have an example of the format used to set up feature flags in a json file. The example below uses the v3 configuration schema which is supported in Microsoft.FeatureManagement version 3. For previous schemas see the configuration [schema details](./docs/schemas/README.md).

``` JavaScript
{
    // Define feature flags in a json configuration file
    "FeatureManagement": {
        "FeatureFlags": {
            "FeatureT": {
                "EnabledFor": [
                    {
                        "Name": "AlwaysOn"
                    }
                ]
            },
            "FeatureU": {
                "EnabledFor": [
                    {
                        "Name": "TimeWindow",
                        "Parameters": {
                            "Start": "Wed, 01 May 2019 13:59:59 GMT",
                            "End": "Mon, 01 July 2019 00:00:00 GMT"
                        }
                    }
                ]
            }
        }
    }
}
```

The `FeatureManagement` section of the json document is used by convention to load feature flag settings. In the section above, we see that we have provided two different features. Features define their feature filters using the `EnabledFor` property. In the feature filters for `FeatureT` we see `AlwaysOn`. This feature filter is built-in and if specified will always enable the feature. The `AlwaysOn` feature filter does not require any configuration so it only has the _Name_ property. `FeatureV` specifies a feature filter named `TimeWindow`. This is an example of a configurable feature filter. We can see in the example that the filter has a parameter's property. This is used to configure the filter. In this case, the start and end times for the feature to be active are configured.

### On/Off Declaration
 
The following snippet demonstrates an alternative way to define a feature that can be used for on/off features. 
``` JavaScript
{
    // Define feature flags in a json configuration file
    "FeatureManagement": {
        "FeatureFlags": {
            "FeatureT": true, // On feature
            "FeatureX": false // Off feature
        }
    }
}
```

### Service Registration

Feature flags rely on .NET Core dependency injection. We can register the feature management services using standard conventions.

``` C#
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddFeatureManagement()
                .AddFeatureFilter<PercentageFilter>()
                .AddFeatureFilter<TimeWindowFilter>();
    }
}
```

This tells the feature manager to use the "FeatureManagement" section from the configuration for feature flag settings. It also registers two built-in feature filters named `PercentageFilter` and `TimeWindowFilter`. When filters are referenced in feature flag settings (appsettings.json) the _Filter_ part of the type name can be omitted.


**Advanced:** The feature manager looks for feature definitions in a configuration section named "FeatureManagement". If the "FeatureManagement" section does not exist, it falls back to the root of the provided configuration.

## Consumption
The simplest use case for feature flags is to do a conditional check for whether a feature is enabled to take different paths in code. The uses cases grow from there as the feature flag API begins to offer extensions into ASP.NET Core.

### Feature Check
The basic form of feature management is checking if a feature is enabled and then performing actions based on the result. This is done through the `IFeatureManager`'s `IsEnabledAsync` method.

``` C#
…
IFeatureManager featureManager;
…
if (await featureManager.IsEnabledAsync(nameof(MyFeatureFlags.FeatureU)))
{
    // Do something
}
```

### Dependency Injection

When using the feature management library with MVC, the `IFeatureManager` can be obtained through dependency injection.

``` C#
public class HomeController : Controller
{
    private readonly IFeatureManager _featureManager;
    
    public HomeController(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }
}
```

### Controllers and Actions
MVC controller and actions can require that a given feature flag, or one of any list of feature flags, be enabled in order to execute. This can be done by using a `FeatureGateAttribute`, which can be found in the `Microsoft.FeatureManagement.Mvc` namespace. 

``` C#
[FeatureGate(MyFeatureFlags.FeatureX)]
public class HomeController : Controller
{
    …
}
```

The `HomeController` above is gated by "FeatureX". "FeatureX" must be enabled before any action the `HomeController` contains can be executed. 

``` C#
[FeatureGate(MyFeatureFlags.FeatureY)]
public IActionResult Index()
{
    return View();
}
```

The `Index` MVC action above requires "FeatureY" to be enabled before it can execute. 

### Disabled Action Handling

When an MVC controller or action is blocked because none of the feature flags it specifies are enabled, a registered `IDisabledFeaturesHandler` will be invoked. By default, a minimalistic handler is registered which returns HTTP 404. This can be overridden using the `IFeatureManagementBuilder` when registering feature flags.

``` C#
public interface IDisabledFeaturesHandler
{
    Task HandleDisabledFeature(IEnumerable<string> features, ActionExecutingContext context);
}
```

### View

In MVC views `<feature>` tags can be used to conditionally render content based on whether a feature is enabled or not.

``` HTML+Razor
<feature name=@nameof(MyFeatureFlags.FeatureX)>
  <p>This can only be seen if 'FeatureX' is enabled.</p>
</feature>
```

The `<feature>` tag requires a tag helper to work. This can be done by adding the feature management tag helper to the _ViewImports.cshtml_ file.
``` HTML+Razor
@addTagHelper *, Microsoft.FeatureManagement.AspNetCore
```

### MVC Filters

MVC action filters can be set up to conditionally execute based on the state of a feature flag. This is done by registering MVC filters in a feature flag aware manner.
The feature management pipeline supports async MVC Action filters, which implement `IAsyncActionFilter`.

``` C#
services.AddMvc(o => 
{
    o.Filters.AddForFeature<SomeMvcFilter>(nameof(MyFeatureFlags.FeatureV));
});
```

The code above adds an MVC filter named `SomeMvcFilter`. This filter is only triggered within the MVC pipeline if the feature flag it specifies, "FeatureV", is enabled.

### Application building

The feature management library can be used to add application branches and middleware that execute conditionally based on feature flag state.

``` C#
app.UseMiddlewareForFeature<ThirdPartyMiddleware>(nameof(MyFeatureFlags.FeatureU));
```

With the above call, the application adds a middleware component that only appears in the request pipeline if the feature flag "FeatureU" is enabled. If the feature flag is enabled/disabled during runtime, the middleware pipeline can be changed dynamically.

This builds off the more generic capability to branch the entire application based on a feature flag.

``` C#
app.UseForFeature(featureName, appBuilder => 
{
    appBuilder.UseMiddleware<T>();
});
```

## Implementing a Feature Filter

Creating a feature filter provides a way to enable feature flags based on criteria that you define. To implement a feature filter, the `IFeatureFilter` interface must be implemented. `IFeatureFilter` has a single method named `EvaluateAsync`. When a feature flag specifies that it can be enabled for a feature filter, the `EvaluateAsync` method is called. If `EvaluateAsync` returns `true` it means the feature flag should be enabled.

Feature filters are registered by the `IFeatureManagementBuilder` when `AddFeatureManagement` is called. These feature filters have access to the services that exist within the service collection that was used to add feature flags. Dependency injection can be used to retrieve these services.

### Parameterized Feature Filters

Some feature filters require parameters to decide whether a feature flag should be turned on or not. For example a browser feature filter may turn on a feature flag for a certain set of browsers. It may be desired that Edge and Chrome browsers enable a feature flag, while Firefox does not. To do this a feature filter can be designed to expect parameters. These parameters would be specified in the feature configuration, and in code would be accessible via the `FeatureFilterEvaluationContext` parameter of `IFeatureFilter.EvaluateAsync`.

``` C#
public class FeatureFilterEvaluationContext
{
    /// <summary>
    /// The name of the feature being evaluated.
    /// </summary>
    public string FeatureName { get; set; }

    /// <summary>
    /// The settings provided for the feature filter to use when evaluating whether the feature should be enabled.
    /// </summary>
    public IConfiguration Parameters { get; set; }
}
```

`FeatureFilterEvaluationContext` has a property named `Parameters`. These parameters represent a raw configuration that the feature filter can use to decide how to evaluate whether the feature flag should be enabled or not. To use the browser feature filter as an example once again, the filter could use `Parameters` to extract a set of allowed browsers that would have been specified for the feature flag and then check if the request is being sent from one of those browsers.

``` C#
[FilterAlias("Browser")]
public class BrowserFilter : IFeatureFilter
{
    … Removed for example

    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        BrowserFilterSettings settings = context.Parameters.Get<BrowserFilterSettings>() ?? new BrowserFilterSettings();

        //
        // Here we would use the settings and see if the request was sent from any of BrowserFilterSettings.AllowedBrowsers
    }
}
```

### Filter Alias Attribute

When a feature filter is registered to be used for a feature flag, the alias used in configuration is the name of the feature filter type with the _filter_ suffix, if any, removed. For example `MyCriteriaFilter` would be referred to as _MyCriteria_ in configuration.

``` JavaScript
"MyFeature": {
    "EnabledFor": [
        {
            "Name": "MyCriteria"
        }
    ]
}
```
This can be overridden through the use of the `FilterAliasAttribute`. A feature filter can be decorated with this attribute to declare the name that should be used in configuration to reference this feature filter within a feature flag.

### Missing Feature Filters

If a feature flag is configured to be enabled for a specific feature filter and that feature filter hasn't been registered, then an exception will be thrown when the feature flag is evaluated. The exception can be disabled by using the feature management options. 

``` C#
services.Configure<FeatureManagementOptions>(options =>
{
    options.IgnoreMissingFeatureFilters = true;
});
```

### Using HttpContext

Feature filters can evaluate whether a feature flag should be enabled based off the properties of an HTTP Request. This is performed by inspecting the HTTP Context. A feature filter can get a reference to the HTTP Context by obtaining an `IHttpContextAccessor` through dependency injection.

``` C#
public class BrowserFilter : IFeatureFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BrowserFilter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }
}
```

The `IHttpContextAccessor` must be added to the dependency injection container on startup for it to be available. It can be registered in the `IServiceCollection` using the following method.

``` C#
public void ConfigureServices(IServiceCollection services)
{
    …
    services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    …
}
```

## Providing a Context For Feature Evaluation

In console applications there is no ambient context such as `HttpContext` that feature filters can acquire and utilize to check if a feature should be on or off. In this case, applications need to provide an object representing a context into the feature management system for use by feature filters. This is done by using `IFeatureManager.IsEnabledAsync<TContext>(string featureName, TContext appContext)`. The appContext object that is provided to the feature manager can be used by feature filters to evaluate the state of a feature flag.

``` C#
MyAppContext context = new MyAppContext
{
    AccountId = current.Id;
}

if (await featureManager.IsEnabledAsync(feature, context))
{
…
}
```

### Contextual Feature Filters

Contextual feature filters implement the `IContextualFeatureFilter<TContext>` interface. These special feature filters can take advantage of the context that is passed in when `IFeatureManager.IsEnabledAsync<TContext>` is called. The `TContext` type parameter in `IContextualFeatureFilter<TContext>` describes what context type the filter is capable of handling. This allows the developer of a contextual feature filter to describe what is required of those who wish to utilize it. Since every type is a descendant of object, a filter that implements `IContextualFeatureFilter<object>` can be called for any provided context. To illustrate an example of a more specific contextual feature filter, consider a feature flag that is enabled if an account is in a configured list of enabled accounts. 

``` C#
public interface IAccountContext
{
    string AccountId { get; set; }
}

[FilterAlias("AccountId")]
class AccountIdFilter : IContextualFeatureFilter<IAccountContext>
{
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureEvaluationContext, IAccountContext accountId)
    {
        //
        // Evaluate if the feature should be on with the help of the provided IAccountContext
    }
}
```

We can see that the `AccountIdFilter` requires an object that implements `IAccountContext` to be provided to be able to evalute the state of a feature flag. When using this feature filter, the caller needs to make sure that the passed in object implements `IAccountContext`.

**Note:** Only a single feature filter interface can be implemented by a single type. Trying to add a feature filter that implements more than a single feature filter interface will result in an `ArgumentException`.

### Built-In Feature Filters

There a few feature filters that come with the `Microsoft.FeatureManagement` package. These feature filters are not added automatically, but can be referenced and registered as soon as the package is registered.

Each of the built-in feature filters have their own parameters. Here is the list of feature filters along with examples.

#### Microsoft.Percentage

This filter provides the capability to enable a feature flag based on a set percentage.

``` JavaScript
"EnhancedPipeline": {
    "EnabledFor": [
        {
            "Name": "Microsoft.Percentage",
            "Parameters": {
                "Value": 50
            }
        }
    ]
}
```

#### Microsoft.TimeWindow

This filter provides the capability to enable a feature flag based on a time window. If only `End` is specified, the feature flag will be considered on until that time. If only start is specified, the feature flag will be considered on at all points after that time.

``` JavaScript
"EnhancedPipeline": {
    "EnabledFor": [
        {
            "Name": "Microsoft.TimeWindow",
            "Parameters": {
                "Start": "Wed, 01 May 2019 13:59:59 GMT",
                "End": "Mon, 01 July 2019 00:00:00 GMT"
            }
        }
    ]
}
```

#### Microsoft.Targeting

This filter provides the capability to enable a feature flag for a target audience. An in-depth explanation of targeting is explained in the [targeting](./README.md#Targeting) section below. The filter parameters include an audience object which describes users, groups, and a default percentage of the user base that should have access to the feature flag. Each group object that is listed in the target audience must also specify what percentage of the group's members should have access. If a user is specified in the users section directly, or if the user is in the included percentage of any of the group rollouts, or if the user falls into the default rollout percentage then that user will have the feature flag enabled.

``` JavaScript
"EnhancedPipeline": {
    "EnabledFor": [
        {
            "Name": "Microsoft.Targeting",
            "Parameters": {
                "Audience": {
                    "Users": [
                        "Jeff",
                        "Alicia"
                    ],
                    "Groups": [
                        {
                            "Name": "Ring0",
                            "RolloutPercentage": 100
                        },
                        {
                            "Name": "Ring1",
                            "RolloutPercentage": 50
                        }
                    ],
                    "DefaultRolloutPercentage": 20
                }
            }
        }
    ]
}
```

### Feature Filter Alias Namespaces

All of the built-in feature filter alias' are in the 'Microsoft' feature filter namespace. This is to prevent conflicts with other feature filters that may share the same simple alias. The segments of a feature filter namespace are split by the '.' character. A feature filter can be referenced by its fully qualified alias such as 'Microsoft.Percentage' or by the last segment which in the case of 'Microsoft.Percentage' is 'Percentage'.

## Targeting

Targeting is a feature management strategy that enables developers to progressively roll out new features to their user base. The strategy is built on the concept of targeting a set of users known as the target _audience_. An audience is made up of specific users, groups, and a designated percentage of the entire user base. The groups that are included in the audience can be broken down further into percentages of their total members.

The following steps demonstrate an example of a progressive rollout for a new 'Beta' feature:

1. Individual users Jeff and Alicia are granted access to the Beta
2. Another user, Mark, asks to opt-in and is included.
3. Twenty percent of a group known as "Ring1" users are included in the Beta.
5. The number of "Ring1" users included in the beta is bumped up to 100 percent.
5. Five percent of the user base is included in the beta.
6. The rollout percentage is bumped up to 100 percent and the feature is completely rolled out.

This strategy for rolling out a feature is built in to the library through the included [Microsoft.Targeting](./README.md#MicrosoftTargeting) feature filter.

## Targeting in a Web Application

An example web application that uses the targeting feature filter is available in the [FeatureFlagDemo](./examples/FeatureFlagDemo) example project.

To begin using the `TargetingFilter` in an application it must be added to the application's service collection just as any other feature filter. Unlike other built in filters, the `TargetingFilter` relies on another service to be added to the application's service collection. That service is an `ITargetingContextAccessor`.

The implementation type used for the `ITargetingContextAccessor` service must be implemented by the application that is using the targeting filter. Here is an example setting up feature management in a web application to use the `TargetingFilter` with an implementation of `ITargetingContextAccessor` called `HttpContextTargetingContextAccessor`.

``` C#
services.AddSingleton<ITargetingContextAccessor, HttpContextTargetingContextAccessor>();

services.AddFeatureManagement();
        .AddFeatureFilter<TargetingFilter>();

```

### ITargetingContextAccessor

To use the `TargetingFilter` in a web application an implementation of `ITargetingContextAccessor` is required. This is because when a targeting evaluation is being performed information such as what user is currently being evaluated is needed. This information is known as the targeting context. Different web applications may extract this information from different places. Some common examples of where an application may pull the targeting context are the request's HTTP context or a database.

An example that extracts targeting context information from the application's HTTP context is included in the [FeatureFlagDemo](./examples/FeatureFlagDemo/HttpContextTargetingContextAccessor.cs) example project. This method relies on the use of `IHttpContextAccessor` which is discussed [here](./README.md#Using-HttpContext).

## Targeting in a Console Application

The targeting filter relies on a targeting context to evaluate whether a feature should be turned on. This targeting context contains information such as what user is currently being evaluated, and what groups the user in. In console applications there is typically no ambient context available to flow this information in to the targeting filter, thus it must be passed directly when `FeatureManager.IsEnabledAsync` is called. This is supported through the use of the `ContextualTargetingFilter`. Applications that need to float the targeting context into the feature manager should use this instead of the `TargetingFilter.`

``` C#
services.AddFeatureManagement()
        .AddFeatureFilter<ContextualTargetingFilter>();
```

Since `ContextualTargetingFilter` is an [`IContextualTargetingFilter<ITargetingContext>`](./README.md#Contextual-Feature-Filters), an implementation of `ITargetingContext` must be passed in to `IFeatureManager.IsEnabledAsync` for it to be able to evaluate and turn a feature on.

``` C#
IFeatureManager fm;
…
// userId and groups defined somewhere earlier in application
TargetingContext targetingContext = new TargetingContext
{
   UserId = userId,
   Groups = groups;
}

await fm.IsEnabledAsync(featureName, targetingContext);
```

The `ContextualTargetingFilter` still uses the feature filter alias [Microsoft.Targeting](./README.md#MicrosoftTargeting), so the configuration for this filter is consistent with what is mentioned in that section.

An example that uses the `ContextualTargetingFilter` in a console application is available in the [TargetingConsoleApp](./examples/TargetingConsoleApp) example project.

## Targeting Evaluation Options

Options are available to customize how targeting evaluation is performed across all features. These options can be configured when setting up feature management.

``` C#
services.Configure<TargetingEvaluationOptions>(options =>
{
    options.IgnoreCase = true;
});
```

## Caching

Feature state is provided by the IConfiguration system. Any caching and dynamic updating is expected to be handled by configuration providers. The feature manager asks IConfiguration for the latest value of a feature's state whenever a feature is checked to be enabled.

### Snapshot
There are scenarios which require the state of a feature to remain consistent during the lifetime of a request. The values returned from the standard `IFeatureManager` may change if the `IConfiguration` source which it is pulling from is updated during the request. This can be prevented by using `IFeatureManagerSnapshot`. `IFeatureManagerSnapshot` can be retrieved in the same manner as `IFeatureManager`. `IFeatureManagerSnapshot` implements the interface of `IFeatureManager`, but it caches the first evaluated state of a feature during a request and will return the same state of a feature during its lifetime. Symmetric functionality is available for dynamic features through the use of `IDynamicFeatureManagerSnapshot`.

## Custom Feature Providers

Implementing a custom feature provider enable developers to pull feature flags from sources such as a database or a feature management service. The included feature provider that is used by default pulls feature flags from .NET Core's configuration system. This allows for features to be defined in an [appsettings.json](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1#jcp) file or in configuration providers like [Azure App Configuration](https://docs.microsoft.com/en-us/azure/azure-app-configuration/quickstart-feature-flag-aspnet-core?tabs=core2x). This behavior can be substituted to provide complete control of where feature definitions are read from.

To customize the loading of feature definitions, one must implement the `IFeatureFlagDefinitionProvider` interface.

``` C#
public interface IFeatureFlagDefinitionProvider
{
        Task<FeatureFlagDefinition> GetFeatureFlagDefinitionAsync(string featureName, CancellationToken cancellationToken = default);

        IAsyncEnumerable<FeatureFlagDefinition> GetAllFeatureFlagDefinitionsAsync(CancellationToken cancellationToken = default);
}
```

To use an implementation of `IFeatureDefinitionProvider` it must be added into the service collection before adding feature management. The following example adds an implementation of `IFeatureDefinitionProvider` named `InMemoryFeatureDefinitionProvider`.

``` C#
services.AddSingleton<IFeatureDefinitionProvider, InMemoryFeatureDefinitionProvider>()
        .AddFeatureManagement()
```

It is also possible to provide custom dynamic feature definitions. This is done by implementing the `IDynamicFeatureDefinitionProvider` interface and registering it as mentioned above.

## Dynamic Features

When new features are being added to an application there may come a time when a feature has multiple different proposed design options. The different options for the design of a feature can be referred to as variants of the feature, and the feature itself can be referred to as a dynamic feature. A dynamic feature is a feature that can have different values (variants) extending beyond a simple on/off flag. A common pattern when rolling out dynamic features is to surface the different variants of a feature to different segments of a user base and to see how each variant is perceived. The most well received variant could be the one that gets rolled out to the entire user base, or if necessary the feature could be scrapped. There could be other reasons to expose different variants of a feature, for example using a different version every day of the week. The goal of this method is to establish a model that can help solve these common patterns that occur when rolling out features that can have different variants.

### Consumption

Dynamic features are accessible through the `IDynamicFeatureManager` interface.

``` C#
public interface IDynamicFeatureManager
{
    IAsyncEnumerable<string> GetDynamicFeatureNamesAsync(CancellationToken cancellationToken = default);

    ValueTask<T> GetVariantAsync<T>(string dynamicFeature, CancellationToken cancellationToken = default);

    ValueTask<T> GetVariantAsync<T, TContext>(string dynamicFeature, TContext context, CancellationToken cancellationToken = default);
}
```

The dynamic feature manager performs a resolution process that takes the name of a feature and returns a strongly typed value to represent the variant's value.

The following steps are performed during the retrieval of a dynamic feature's variant
1. Lookup the configuration of the specified dynamic feature to find the registered variants
2. Assign one of the registered variants to be used.
3. Resolve typed value based off of the assigned variant.

The dynamic feature manager is made available by using the `AddFeatureManagement` call detailed in the [service registration](./README.md#Service-Registration) section. Make sure to add any required feature variant assigners referenced by dynamic features within the application by using `AddFeatureVariantAssigner`.

``` C#
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Assigners;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddFeatureManagement()
                .AddFeatureVariantAssigner<TargetingFeatureVariantAssigner>();
    }
}
```

### Usage Example

One possible example of when variants may be used is in a web application when there is a desire to test different visuals. In the following examples a mock of how one might assign different variants of a web page background to their users is shown.

``` C#
//
// Modify view based off multiple possible variants
model.BackgroundUrl = featureVariantManager.GetVariantAsync<string>("HomeBackground", cancellationToken);

return View(model);
```

### Configuring a Dynamic Feature

Dynamic features can be configured in a configuration file similarly to feature flags. Instead of being defined in the `FeatureManagement:FeatureFlags` section, they are defined in the `FeatureManagement:DynamicFeatures` section. Additionally, dynamic features have the following properties.

* Assigner: The assigner that should be used to select which variant should be used any time this feature is accessed.
* Variants: The different variants of the dynamic feature.
  * Default: Whether the variant should be used if no variant could be explicitly assigned.
  * Configuration Reference: A reference to the configuration of the variant to be used as typed options in the application.
  * Assignment Parameters: The parameters used in the assignment process to determine if this variant should be used.

An example of a dynamic feature named "ShoppingCart" is shown below.

``` JavaScript
{
    "FeatureManagement":
    {
        "DynamicFeatures": {
            "ShoppingCart": {
                "Assigner": "Targeting",
                "Variants": [
                    {
                        "Default": true,
                        "Name": "Big",
                        "ConfigurationReference": "ShoppingCart:Big",
                        "AssignmentParameters": {
                            "Audience": {
                                "Users": [
                                    "Alec",
                                ],
                                "Groups": [
                                ]
                            }
                        }
                    },
                    {
                        "Name": "Small",
                        "ConfigurationReference": "ShoppingCart:Small",
                        "AssignmentParameters": {
                            "Audience": {
                                "Users": [
                                ],
                                "Groups": [
                                    {
                                        "Name": "Ring1",
                                        "RolloutPercentage": 50
                                    }
                                ],
                                "DefaultRolloutPercentage": 30
                            }
                        }
                    }
                ]
            }
        }
    },
    "ShoppingCart": {
        "Big": {
            "Size": 400,
            "Color": "green"
        },
        "Small": {
            "Size": 150,
            "Color": "gray"
        }
    }
}
```

In the example above we see the declaration of a dynamic feature in a json configuration file. The dynamic feature is defined in the `FeatureManagement:DynamicFeatures` section of configuration. The name of this dynamic feature is `ShoppingCart`. A dynamic feature must declare a feature variant assigner that should be used to select a variant when requested. In this case the built-in `Targeting` feature variant assigner is used. The dynamic feature has two different variants that are available to the application. One variant is named `Big` and the other is named `Small`. Each variant contains a configuration reference denoted by the `ConfigurationReference` property. The configuration reference is a pointer to a section of application configuration that contains the options that should be used for that variant. The variant also contains assignment parameters denoted by the `AssignmentParameters` property. The assignment parameters are used by the assigner associated with the dynamic feature. The assigner reads the assignment parameters at run time when a variant of the dynamic feature is requested to choose which variant should be returned. 

An application that is configured with this `ShoppingCart` dynamic feature may request the value of a variant of the feature at runtime through the use of `IDynamicFeatureManager.GetVariantAsync`. The dynamic feature uses targeting for [variant assignment](./README.md#Feature-Variant-Assignment) so each of the variants' assignment parameters specify a target audience that should receive the variant. For a walkthrough of how the targeting assigner would choose a variant in this scenario reference the [Microsoft.Targeting Assigner](./README.md#Microsoft.Targeting-Assigner) section. When the feature manager chooses one of the variants it resolves the value of the variant by resolving the configuration reference declared in the variant. The example above includes the configuration that is referenced by the `ConfigurationReference` of each variant.

### Feature Variant Assignment

When requesting the value of a dynamic feature the feature manager needs to determine which variant of the feature should be used. The act of choosing which variant should be used is called assignment. A built-in method of assignment is provided that allows the variants of a dynamic features to be assigned to segments of an application's audience. This is the same [targeting](./README.md#Microsoft.Targeting-Assigner) strategy introduced by the targeting feature filter.

To perform assignment the feature manager uses components known as feature variant assigners. Feature variant assigners have the job of choosing which of the variants of a dynamic feature should be used when the value of a dynamic feature is requested. Each variant of a dynamic feature declares assignment parameters so that when an assigner is invoked the assigner can tell under which conditions each variant should be selected. It is possible that an assigner is unable to choose between the list of available variants based off of their configured assignment parameters. In this case the feature manager chooses the **default variant**. The default variant is a variant that is marked explicitly as default. It is required to have a default variant when configuring a dynamic feature in order to handle the possibility that an assigner is not able to select a variant of a dynamic feature.

### Custom Assignment

There may come a time when custom criteria is needed to decide which variant of a feature should be assigned when a feature is referenced. This is made possible by an extensibility model that allows the act of assignment to be overriden. Every feature registered in the feature management system that uses feature variants specifies what assigner should be used to choose a variant.


``` C#
    public interface IFeatureVariantAssigner : IFeatureVariantAssignerMetadata
    {
        /// <summary>
        /// Assign a variant of a feature to be used based off of customized criteria.
        /// </summary>
        /// <param name="variantAssignmentContext">Information provided by the system to be used during the assignment process.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The variant that should be assigned for a given feature.</returns>
        ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext variantAssignmentContext, CancellationToken cancellationToken);
    }
}
```

An example implementation can be found in [this example](./examples/CustomAssignmentConsoleApp/RecurringAssigner.cs).

### Built-In Feature Variant Assigners

There is a built-in feature variant assigner that uses targeting that comes with the `Microsoft.FeatureManagement` package. This assigner is not added automatically, but it can be referenced and registered as soon as the package is registered.

#### Microsoft.Targeting Assigner

This feature variant assigner provides the capability to assign the variants of a dynamic feature to targeted audiences. An in-depth explanation of targeting is explained in the [targeting](./README.md#Targeting) section.

The assignment parameters used by the targeting feature variant assigner include an audience object which describes users, groups, and a default percentage of the user base that should receive the associated variant. Each group object that is listed in the target audience must also specify what percentage of the group's members should have receive the variant. If a user is specified in the users section directly, or if the user is in the included percentage of any of the group rollouts, or if the user falls into the default rollout percentage then that user will receive the associated variant.

``` JavaScript
"ShoppingCart": {
    "Assigner": "Targeting",
    "Variants": [
        {
            "Default": true,
            "Name": "Big",
            "ConfigurationReference": "ShoppingCart:Big",
            "AssignmentParameters": {
                "Audience": {
                    "Users": [
                        "Alec",
                    ],
                    "Groups": [
                        {
                            "Name": "Ring0",
                            "RolloutPercentage": 100
                        },
                        {
                            "Name": "Ring1",
                            "RolloutPercentage": 50
                        }
                    ]
                }
            }
        },
        {
            "Name": "Small",
            "ConfigurationReference": "ShoppingCart:Small",
            "AssignmentParameters": {
                "Audience": {
                    "Users": [
                        "Susan",
                    ],
                    "Groups": [
                        {
                            "Name": "Ring1",
                            "RolloutPercentage": 50
                        }
                    ],
                    "DefaultRolloutPercentage": 80
                }
            }
        }
    ]
}
```

Based on the configured audiences for the variants included in this feature, if the application is executing under the context of a user named `Alec` then the value of the `Big` variant will be returned. If the application is executing under the context of a user named `Susan` then the value of the `Small` variant will be returned. If a user match does not occur, then group matches are evaluated. If the application is executing under the context of a user in the group `Ring0` then the `Big` variant will be returned. If the user's group is `Ring1` instead, then the user has  a 50% chance between being assigned to `Big` or `Small`. If there is not user match nor group match then the default rollout percentage is used. In this case, 80% of unmatched users will get the `Small` variant leaving the other 20% to get the `Big` variant since it is marked as the `Default`.

Example usage of this assigner can be found in the [FeatureFlagDemo example](./examples/FeatureFlagDemo/Startup.cs#L63).

When using the targeting feature variant assigner, make sure to register it as well as an implementation of [ITargetingContextAccessor](./README.md#ITargetingContextAccessor).

``` C#
services.AddSingleton<ITargetingContextAccessor, HttpContextTargetingContextAccessor>();

services.AddFeatureManagement();
        .AddFeatureVariantAssigner<TargetingFeatureVariantAssigner>();
```

### Variant Value Resolution

When a variant of a dynamic feature has been chosen, the feature management system needs to resolve the value associated with that variant. A feature variant can reference configuration values through the `ConfigurationReference` property of their configuration to be used as the value of the feature. In the "[Configuring a Dynamic Feature](./README.md#Configuring-a-Dynamic-Feature)" section we see a dynamic feature named "ShoppingCart". The first variant of the feature, named "Big", has a configuration reference to the `ShoppingCart:Big` configuration section. The referenced section is shown below.

``` Javascript
    "ShoppingCart": {
        "Big": {
            "Size": 400,
            "Color": "green"
        }
    }
```

The feature management system resolves the configuration reference and binds the resolved configuration section to the type specfied when a dynamic feature's value is requested. This is performed by an implementation of the  `IFeatureVariantOptionsResolver`. By providing a custom implementation of `IFeatureVariantOptionsResolver`, a developer can resolve configuration references from sources other than configuration.

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
