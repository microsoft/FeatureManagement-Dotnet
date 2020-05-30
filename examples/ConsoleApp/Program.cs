// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Consoto.Banking.AccountService.FeatureFilters;

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
                .AddFeatureFilter<PercentageFilter>()
                .AddFeatureFilter<AccountIdFilter>();

            //
            // Get the feature manager from application services
            using (var serviceProvider = services.BuildServiceProvider())
            {
                var featureManager = serviceProvider.GetRequiredService<IFeatureManager>();

                var accounts = new List<string> {"abc", "adef", "abcdefghijklmnopqrstuvwxyz"};

                //
                // Mimic work items in a task-driven console application
                foreach (var account in accounts)
                {
                    const string featureName = "Beta";

                    //
                    // Check if feature enabled
                    //
                    var accountServiceContext = new AccountServiceContext {AccountId = account};

                    var enabled = await featureManager.IsEnabledAsync(featureName, accountServiceContext);

                    //
                    // Output results
                    Console.WriteLine(
                        $"The {featureName} feature is " +
                        $"{(enabled ? "enabled" : "disabled")} for the '{account}' account.");
                }
            }
        }
    }
}
