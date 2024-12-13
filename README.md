# .NET Feature Management

[![Microsoft.FeatureManagement](https://img.shields.io/nuget/v/Microsoft.FeatureManagement?label=Microsoft.FeatureManagement)](https://www.nuget.org/packages/Microsoft.FeatureManagement)
[![Microsoft.FeatureManagement.AspNetCore](https://img.shields.io/nuget/v/Microsoft.FeatureManagement.AspNetCore?label=Microsoft.FeatureManagement.AspNetCore)](https://www.nuget.org/packages/Microsoft.FeatureManagement.AspNetCore)

Feature management provides a way to develop and expose application functionality based on features. Many applications have special requirements when a new feature is developed such as when the feature should be enabled and under what conditions. This library provides a way to define these relationships, and also integrates into common .NET code patterns to make exposing these features possible. 

## Get started

[**Quickstart**](https://learn.microsoft.com/azure/azure-app-configuration/quickstart-feature-flag-dotnet): A quickstart guide is available to learn how to integrate feature flags from *Azure App Configuration* into your .NET applications.

[**Feature Reference**](https://learn.microsoft.com/azure/azure-app-configuration/feature-management-dotnet-reference): This document provides a full feature rundown.

[**API reference**](https://go.microsoft.com/fwlink/?linkid=2091700): This API reference details the API surface of the libraries contained within this repository.

## Examples

* [.NET Console App](./examples/ConsoleApp)
* [.NET Console App with Targeting](./examples/TargetingConsoleApp)
* [ASP.NET Core Web App (Razor Page)](./examples/RazorPages)
* [ASP.NET Core Web App (MVC)](./examples/FeatureFlagDemo)
* [Blazor Server App](./examples/BlazorServerApp)
* [ASP.NET Core Web App with Variants and Telemetry](./examples/VariantAndTelemetryDemo)
* [ASP.NET Core Web App with Variant Service](./examples/VariantServiceDemo)

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
