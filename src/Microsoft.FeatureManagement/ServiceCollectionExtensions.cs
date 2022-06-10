// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

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
            services.TryAddSingleton<IFeatureFlagDefinitionProvider, ConfigurationFeatureFlagDefinitionProvider>();

            services.TryAddSingleton<IDynamicFeatureDefinitionProvider, ConfigurationDynamicFeatureDefinitionProvider>();

            services.TryAddSingleton<IFeatureVariantOptionsResolver, ConfigurationFeatureVariantOptionsResolver>();

            services.TryAddSingleton<IFeatureManager, FeatureManager>();

            services.TryAddSingleton<IDynamicFeatureManager, DynamicFeatureManager>();

            services.TryAddSingleton<ISessionManager, EmptySessionManager>();

            services.TryAddScoped<IFeatureManagerSnapshot, FeatureManagerSnapshot>();

            services.TryAddScoped<IDynamicFeatureManagerSnapshot, DynamicFeatureManagerSnapshot>();

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

            services.AddSingleton<IFeatureFlagDefinitionProvider>(new ConfigurationFeatureFlagDefinitionProvider(configuration));

            services.AddSingleton<IDynamicFeatureDefinitionProvider>(new ConfigurationDynamicFeatureDefinitionProvider(configuration));

            return services.AddFeatureManagement();
        }
    }
}
