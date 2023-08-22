// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Extensions used to add feature management functionality.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds required feature management services.
        /// </summary>
        /// <param name="services">The service collection that feature management services are added to.</param>
        /// <returns>A <see cref="IFeatureManagementBuilder"/> that can be used to customize feature management functionality.</returns>
        public static IFeatureManagementBuilder AddFeatureManagement(this IServiceCollection services)
        {
            services.AddLogging();

            //
            // Add required services
            services.TryAddSingleton<IFeatureDefinitionProvider, ConfigurationFeatureDefinitionProvider>();

            services.AddSingleton<FeatureManager>();

            services.TryAddSingleton<IFeatureManager>(sp => sp.GetRequiredService<FeatureManager>());

            services.TryAddSingleton<IFeatureManager>(sp =>
            new FeatureManager(
                sp.GetRequiredService<IFeatureDefinitionProvider>(),
                sp.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>(),
                sp.GetRequiredService<IEnumerable<ISessionManager>>(),
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<IOptions<FeatureManagementOptions>>(),
                sp.GetRequiredService<IOptions<TargetingEvaluationOptions>>())
                {
                    Configuration = sp.GetService<IConfiguration>(), // May or may not exist in DI
                    TargetingContextAccessor = sp.GetService<ITargetingContextAccessor>()
                });

            services.TryAddSingleton<IVariantFeatureManager>(sp =>
            new FeatureManager(
                sp.GetRequiredService<IFeatureDefinitionProvider>(),
                sp.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>(),
                sp.GetRequiredService<IEnumerable<ISessionManager>>(),
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<IOptions<FeatureManagementOptions>>(),
                sp.GetRequiredService<IOptions<TargetingEvaluationOptions>>())
            {
                Configuration = sp.GetService<IConfiguration>(), // May or may not exist in DI
                TargetingContextAccessor = sp.GetService<ITargetingContextAccessor>()
            });
            services.AddSingleton<ISessionManager, EmptySessionManager>();

            services.AddScoped<FeatureManagerSnapshot>();

            services.TryAddScoped<IFeatureManagerSnapshot>(sp => sp.GetRequiredService<FeatureManagerSnapshot>());

            services.TryAddScoped<IVariantFeatureManagerSnapshot>(sp => sp.GetRequiredService<FeatureManagerSnapshot>());

            return new FeatureManagementBuilder(services);
        }

        /// <summary>
        /// Adds required feature management services.
        /// </summary>
        /// <param name="services">The service collection that feature management services are added to.</param>
        /// <param name="configuration">A specific <see cref="IConfiguration"/> instance that will be used to obtain feature settings.</param>
        /// <returns>A <see cref="IFeatureManagementBuilder"/> that can be used to customize feature management functionality.</returns>
        public static IFeatureManagementBuilder AddFeatureManagement(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.AddSingleton<IFeatureDefinitionProvider>(new ConfigurationFeatureDefinitionProvider(configuration));

            return services.AddFeatureManagement();
        }
    }
}
