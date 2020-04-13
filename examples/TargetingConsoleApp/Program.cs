// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Consoto.Banking.AccountService.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Consoto.Banking.HelpDesk
{
    class Program
    {
        public static async Task Main(string[] args)
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
                    //
                    TargetingContext targetingContext = new TargetingContext
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
        }
    }
}
