// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;

//
// Setup configuration
IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var featureManager = new FeatureManager(new ConfigurationFeatureDefinitionProvider(configuration))
{
    FeatureFilters = new List<IFeatureFilterMetadata> { new AccountIdFilter() }
};

var accounts = new List<string>()
{
    "abc",
    "adef",
    "abcdefghijklmnopqrstuvwxyz"
};

//
// Mimic work items in a task-driven console application
foreach (var account in accounts)
{
    const string FeatureName = "Beta";

    //
    // Check if feature enabled
    //
    var accountServiceContext = new AccountServiceContext
    {
        AccountId = account
    };

    bool enabled = await featureManager.IsEnabledAsync(FeatureName, accountServiceContext);

    //
    // Output results
    Console.WriteLine($"The {FeatureName} feature is {(enabled ? "enabled" : "disabled")} for the '{account}' account.");
}
