# ParametersObject Console App

This example demonstrates how to use the `ParametersObject` property on `FeatureFilterConfiguration` to supply filter settings directly from a custom `IFeatureDefinitionProvider`.

## Overview

When implementing a custom `IFeatureDefinitionProvider` that sources feature definitions from alternative backends (e.g., databases, REST APIs), you no longer need to construct an `IConfiguration` object to pass filter parameters. Instead, you can assign settings like `TargetingFilterSettings` directly to `ParametersObject`.

## Key Concept

```csharp
new FeatureFilterConfiguration
{
    Name = "Microsoft.Targeting",
    ParametersObject = new TargetingFilterSettings
    {
        Audience = new Audience
        {
            Users = new List<string> { "Jeff", "Anne" },
            Groups = new List<GroupRollout> { ... },
            DefaultRolloutPercentage = 20
        }
    }
}
```

This eliminates the need for verbose `IConfiguration` construction with magic string keys.

## Running

```
dotnet run
```
