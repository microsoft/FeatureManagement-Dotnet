// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace FeatureFlagDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //
            // Opt-in to use new schema with features received from Azure App Configuration
            Environment.SetEnvironmentVariable("AZURE_APP_CONFIGURATION_FEATURE_MANAGEMENT_SCHEMA_VERSION", "2");

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((ctx, builder) =>
                {
                    var settings = builder.Build();

                    if (!string.IsNullOrEmpty(settings["AppConfiguration:ConnectionString"]))
                    {
                        //
                        // This section can be used to pull feature flag configuration from Azure App Configuration
                        builder.AddAzureAppConfiguration(o =>
                        {
                            o.Connect(settings["AppConfiguration:ConnectionString"]);

                            o.Select(KeyFilter.Any);

                            o.UseFeatureFlags();
                        });
                    }
                })
                .UseStartup<Startup>();
        }
    }
}
