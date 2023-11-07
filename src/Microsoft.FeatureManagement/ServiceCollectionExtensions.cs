// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Caching.Memory;
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
        /// Adds singleton <see cref="FeatureManager"/> and other required feature management services.
        /// </summary>
        /// <param name="services">The service collection that feature management services are added to.</param>
        /// <returns>A <see cref="IFeatureManagementBuilder"/> that can be used to customize feature management functionality.</returns>
        /// <exception cref="FeatureManagementException">Thrown if <see cref="FeatureManager"/> has been registered as scoped.</exception>
        public static IFeatureManagementBuilder AddFeatureManagement(this IServiceCollection services)
        {
            if (services.Any(descriptor => descriptor.ServiceType == typeof(FeatureManager) && descriptor.Lifetime == ServiceLifetime.Scoped))
            {
                throw new FeatureManagementException(
                    FeatureManagementError.Conflict,
                    "Scoped feature management has been registered.");
            }

            services.AddLogging();

            services.AddMemoryCache();

            //
            // Add required services
            services.TryAddSingleton<IFeatureDefinitionProvider, ConfigurationFeatureDefinitionProvider>();

            services.AddSingleton(sp => new FeatureManager(
                sp.GetRequiredService<IFeatureDefinitionProvider>(),
                sp.GetRequiredService<IOptions<FeatureManagementOptions>>().Value)
            {
                FeatureFilters = sp.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>(),
                SessionManagers = sp.GetRequiredService<IEnumerable<ISessionManager>>(),
                Cache = sp.GetRequiredService<IMemoryCache>(),
                Logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<FeatureManager>()
            });

            services.TryAddSingleton<IFeatureManager>(sp => sp.GetRequiredService<FeatureManager>());

            services.AddScoped<FeatureManagerSnapshot>();

            services.TryAddScoped<IFeatureManagerSnapshot>(sp => sp.GetRequiredService<FeatureManagerSnapshot>());

            var builder = new FeatureManagementBuilder(services);
            
            //
            // Add built-in feature filters
            builder.AddFeatureFilter<PercentageFilter>();

            builder.AddFeatureFilter<TimeWindowFilter>();

            builder.AddFeatureFilter<ContextualTargetingFilter>();

            return builder;
        }

        /// <summary>
        /// Adds singleton <see cref="FeatureManager"/> and other required feature management services.
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

        /// <summary>
        /// Adds scoped <see cref="FeatureManager"/> and other required feature management services.
        /// </summary>
        /// <param name="services">The service collection that feature management services are added to.</param>
        /// <returns>A <see cref="IFeatureManagementBuilder"/> that can be used to customize feature management functionality.</returns>
        /// <exception cref="FeatureManagementException">Thrown if <see cref="FeatureManager"/> has been registered as singleton.</exception>
        public static IFeatureManagementBuilder AddScopedFeatureManagement(this IServiceCollection services)
        {
            if (services.Any(descriptor => descriptor.ServiceType == typeof(FeatureManager) && descriptor.Lifetime == ServiceLifetime.Singleton))
            {
                throw new FeatureManagementException(
                    FeatureManagementError.Conflict,
                    "Singleton feature management has been registered.");
            }

            services.AddLogging();

            services.AddMemoryCache();

            //
            // Add required services
            services.TryAddSingleton<IFeatureDefinitionProvider, ConfigurationFeatureDefinitionProvider>();

            services.AddScoped(sp => new FeatureManager(
                sp.GetRequiredService<IFeatureDefinitionProvider>(),
                sp.GetRequiredService<IOptions<FeatureManagementOptions>>().Value)
            {
                FeatureFilters = sp.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>(),
                SessionManagers = sp.GetRequiredService<IEnumerable<ISessionManager>>(),
                Cache = sp.GetRequiredService<IMemoryCache>(),
                Logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<FeatureManager>()
            });

            services.TryAddScoped<IFeatureManager>(sp => sp.GetRequiredService<FeatureManager>());

            services.AddScoped<FeatureManagerSnapshot>();

            services.TryAddScoped<IFeatureManagerSnapshot>(sp => sp.GetRequiredService<FeatureManagerSnapshot>());

            var builder = new FeatureManagementBuilder(services);

            //
            // Add built-in feature filters
            builder.AddFeatureFilter<PercentageFilter>();

            builder.AddFeatureFilter<TimeWindowFilter>();

            builder.AddFeatureFilter<ContextualTargetingFilter>();

            return builder;
        }

        /// <summary>
        /// Adds scoped <see cref="FeatureManager"/> and other required feature management services.
        /// </summary>
        /// <param name="services">The service collection that feature management services are added to.</param>
        /// <param name="configuration">A specific <see cref="IConfiguration"/> instance that will be used to obtain feature settings.</param>
        /// <returns>A <see cref="IFeatureManagementBuilder"/> that can be used to customize feature management functionality.</returns>
        public static IFeatureManagementBuilder AddScopedFeatureManagement(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.AddSingleton<IFeatureDefinitionProvider>(sp => new ConfigurationFeatureDefinitionProvider(configuration, sp.GetRequiredService<ILoggerFactory>()));

            return services.AddScopedFeatureManagement();
        }
    }
}
