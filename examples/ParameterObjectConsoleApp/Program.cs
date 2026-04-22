// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using ParameterObjectConsoleApp;

//
// Create a feature manager using a custom definition provider
// that supplies targeting filter settings directly via ParametersObject.
var featureManager = new FeatureManager(new InMemoryFeatureDefinitionProvider())
{
    FeatureFilters = new List<IFeatureFilterMetadata> { new ContextualTargetingFilter() }
};

//
// Simulate evaluating the "Beta" feature for different users
var users = new List<(string UserId, List<string> Groups)>
{
    ("Jeff", new List<string> { "TeamMembers" }),       // Targeted by user name
    ("Anne", new List<string> { "Management" }),        // Targeted by user name
    ("Sam", new List<string> { "Management" }),         // Targeted by group (100% rollout)
    ("Alice", new List<string> { "TeamMembers" }),      // May be targeted by group (45% rollout)
    ("Bob", new List<string> { "External" })            // Only targeted by default rollout (20%)
};

foreach (var (userId, groups) in users)
{
    const string FeatureName = "Beta";

    var targetingContext = new TargetingContext
    {
        UserId = userId,
        Groups = groups
    };

    bool enabled = await featureManager.IsEnabledAsync(FeatureName, targetingContext);

    Console.WriteLine($"The {FeatureName} feature is {(enabled ? "enabled" : "disabled")} for the user '{userId}'.");
}
