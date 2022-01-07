﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Consoto.Banking.AccountService.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Assigners;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
                    .AddFeatureFilter<ContextualTargetingFilter>()
                    .AddFeatureVariantAssigner<ContextualTargetingFeatureVariantAssigner>();

            IUserRepository userRepository = new InMemoryUserRepository();

            //
            // Get the feature manager from application services
            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManager>();
                IDynamicFeatureManager dynamicFeatureManager = serviceProvider.GetRequiredService<IDynamicFeatureManager>();

                //
                // We'll simulate a task to run on behalf of each known user
                // To do this we enumerate all the users in our user repository
                IEnumerable<string> userIds = InMemoryUserRepository.Users.Select(u => u.Id);

                //
                // Mimic work items in a task-driven console application
                foreach (string userId in userIds)
                {
                    const string FeatureFlagName = "Beta";
                    const string DynamicFeatureName = "ShoppingCart";

                    //
                    // Get user
                    User user = await userRepository.GetUser(userId);

                    //
                    // Check if feature enabled
                    TargetingContext targetingContext = new TargetingContext
                    {
                        UserId = user.Id,
                        Groups = user.Groups
                    };

                    //
                    // Evaluate feature flag using targeting
                    bool enabled = await featureManager
                        .IsEnabledAsync<TargetingContext>(
                            FeatureFlagName, 
                            targetingContext,
                            CancellationToken.None);

                    //
                    // Retrieve feature variant using targeting
                    CartOptions cartOptions = await dynamicFeatureManager
                        .GetVariantAsync<CartOptions, TargetingContext>(
                            DynamicFeatureName,
                            targetingContext,
                            CancellationToken.None);

                    //
                    // Output results
                    Console.WriteLine($"The {FeatureFlagName} feature is {(enabled ? "enabled" : "disabled")} for the user '{userId}'.");

                    Console.WriteLine($"User {user.Id} has a {cartOptions.Color} cart with a size of {cartOptions.Size} pixels.");
                }
            }
        }
    }
}
