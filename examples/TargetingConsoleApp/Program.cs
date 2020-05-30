// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using System;
using System.Linq;
using System.Threading.Tasks;
using Consoto.Banking.AccountService.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;

namespace Consoto.Banking.AccountService
{
    internal class Program
    {
        public static async Task Main()
        {
            //
            // Setup configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            //
            // Setup application services + feature management
            IServiceCollection services = new ServiceCollection();

            services.AddSingleton(configuration)
                .AddFeatureManagement()
                .AddFeatureFilter<ContextualTargetingFilter>();

            IUserRepository userRepository = new InMemoryUserRepository();

            //
            // Get the feature manager from application services
            using (var serviceProvider = services.BuildServiceProvider())
            {
                var featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

                //
                // We'll simulate a task to run on behalf of each known user
                // To do this we enumerate all the users in our user repository
                var userIds = InMemoryUserRepository.Users.Select(u => u.Id);

                //
                // Mimic work items in a task-driven console application
                foreach (var userId in userIds)
                {
                    const string featureName = "Beta";

                    //
                    // Get user
                    var user = await userRepository.GetUser(userId);

                    //
                    // Check if feature enabled
                    var targetingContext = new TargetingContext {UserId = user.Id, Groups = user.Groups};

                    var enabled = await featureManager.IsEnabledAsync(featureName, targetingContext);

                    //
                    // Output results
                    Console.WriteLine(
                        $"The {featureName} feature is {(enabled ? "enabled" : "disabled")} for the user '{userId}'.");
                }
            }
        }
    }
}
