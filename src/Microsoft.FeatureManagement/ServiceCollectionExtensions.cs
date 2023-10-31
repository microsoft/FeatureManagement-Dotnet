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
using System.Linq;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Extensions used to add feature management functionality.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds required feature management services and built-in feature filters.
        /// </summary>
        /// <param name="services">The service collection that feature management services are added to.</param>
        /// <returns>A <see cref="IFeatureManagementBuilder"/> that can be used to customize feature management functionality.</returns>
        public static IFeatureManagementBuilder AddFeatureManagement(this IServiceCollection services)
        {
            services.AddLogging();

            //
            // Add required services
            services.TryAddSingleton<IFeatureDefinitionProvider, ConfigurationFeatureDefinitionProvider>();

            services.AddSingleton(sp => new FeatureManager(
                sp.GetRequiredService<IFeatureDefinitionProvider>(),
                sp.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>(),
                sp.GetRequiredService<IEnumerable<ISessionManager>>(),
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<IOptions<FeatureManagementOptions>>(),
                sp.GetRequiredService<IOptions<TargetingEvaluationOptions>>())
            {
                Configuration = sp.GetService<IConfiguration>(),
                TargetingContextAccessor = sp.GetService<ITargetingContextAccessor>(),
                TelemetryPublishers = sp.GetService<IOptions<FeatureManagementOptions>>()?.Value.TelemetryPublisherFactories?
                    .Select(factory => factory(sp))
                    .ToList()
            });

            services.TryAddSingleton<IFeatureManager>(sp => sp.GetRequiredService<FeatureManager>());

            services.TryAddSingleton<IVariantFeatureManager>(sp => sp.GetRequiredService<FeatureManager>());

            services.AddSingleton<ISessionManager, EmptySessionManager>();

            services.AddScoped<FeatureManagerSnapshot>();

            services.TryAddScoped<IFeatureManagerSnapshot>(sp => sp.GetRequiredService<FeatureManagerSnapshot>());

            services.TryAddScoped<IVariantFeatureManagerSnapshot>(sp => sp.GetRequiredService<FeatureManagerSnapshot>());

            var builder = new FeatureManagementBuilder(services);
            
            //
            // Add built-in feature filters
            builder.AddFeatureFilter<PercentageFilter>();

            builder.AddFeatureFilter<TimeWindowFilter>();

            builder.AddFeatureFilter<ContextualTargetingFilter>();

            return builder;
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

            services.AddSingleton<IFeatureDefinitionProvider>(sp => new ConfigurationFeatureDefinitionProvider(configuration, sp.GetRequiredService<ILoggerFactory>()));

            return services.AddFeatureManagement();
        }
    }
}
