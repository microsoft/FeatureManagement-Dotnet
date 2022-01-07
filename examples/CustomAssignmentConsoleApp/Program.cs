// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Consoto.Banking.AccountService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using System;
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
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            //
            // Setup application services + feature management
            IServiceCollection services = new ServiceCollection();

            services.AddSingleton(typeof(IFeatureVariantAssignerMetadata), typeof(RecurringAssigner));

            services.AddSingleton(configuration)
                    .AddFeatureManagement()
                    .AddFeatureVariantAssigner<RecurringAssigner>();

            //
            // Get the feature manager from application services
            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                IDynamicFeatureManager dynamicFeatureManager = serviceProvider.GetRequiredService<IDynamicFeatureManager>();

                DailyDiscountOptions discountOptions = await dynamicFeatureManager
                    .GetVariantAsync<DailyDiscountOptions>("DailyDiscount", CancellationToken.None);

                //
                // Output results
                Console.WriteLine($"Today there is a {discountOptions.Discount}% discount on {discountOptions.ProductName}!");
            }
        }
    }
}
