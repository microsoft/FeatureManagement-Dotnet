using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using TargetingConsoleApp.Identity;

//
// Setup configuration
IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

//
// Setup application services + feature management
IServiceCollection services = new ServiceCollection();

services.AddSingleton(configuration)
        .AddFeatureManagement();

IUserRepository userRepository = new InMemoryUserRepository();

//
// Get the feature manager from application services
using (ServiceProvider serviceProvider = services.BuildServiceProvider())
{
    IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

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
}