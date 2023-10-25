// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace FeatureFlagDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
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
