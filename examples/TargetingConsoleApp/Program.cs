// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using TargetingConsoleApp.Identity;

//
// Setup configuration
IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

IFeatureDefinitionProvider featureDefinitionProvider = new ConfigurationFeatureDefinitionProvider(configuration);

var featureManager = new FeatureManager(featureDefinitionProvider)
{
    FeatureFilters = new List<IFeatureFilterMetadata> { new ContextualTargetingFilter() }
};

var userRepository = new InMemoryUserRepository();

//
// We'll simulate a task to run on behalf of each known user
// To do this we enumerate all the users in our user repository
IEnumerable<string> userIds = InMemoryUserRepository.Users.Select(u => u.Id);

//
// Mimic work items in a task-driven console application
foreach (string userId in userIds)
{
    const string FeatureName = "Beta";

    //
    // Get user
    User user = await userRepository.GetUser(userId);

    //
    // Check if feature enabled
    var targetingContext = new TargetingContext
    {
        UserId = user.Id,
        Groups = user.Groups
    };

    bool enabled = await featureManager.IsEnabledAsync(FeatureName, targetingContext);

    //
    // Output results
    Console.WriteLine($"The {FeatureName} feature is {(enabled ? "enabled" : "disabled")} for the user '{userId}'.");
}
