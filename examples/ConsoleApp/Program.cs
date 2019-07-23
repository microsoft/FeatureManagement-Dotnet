// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Consoto.Banking.AccountServer;
using Consoto.Banking.AccountServer.FeatureManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;

namespace Contoso.Banking.AccountServer
{
    class Program
    {
        static void Main(string[] args)
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
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            IContextualFeatureManager contextualFeatureManager = serviceProvider.GetRequiredService<IContextualFeatureManager>();

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
                var accountIdContext = new AccountServerContext();

                accountIdContext.AccountId = account;

                bool enabled = contextualFeatureManager.IsEnabled(FeatureName, accountIdContext);

                //
                // Output results
                Console.WriteLine($"The {FeatureName} feature is {(enabled ? "enabled" : "disabled")} for the '{account}' account.");
            }
        }
    }
}
