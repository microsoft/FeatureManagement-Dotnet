// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Telemetry;
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

            services.TryAddSingleton<IFeatureManager>(sp =>
                new FeatureManager(
                    sp.GetRequiredService<IFeatureDefinitionProvider>(),
                    sp.GetRequiredService<IEnumerable<IFeatureFilterMetadata>>(),
                    sp.GetRequiredService<IEnumerable<ISessionManager>>(),
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetRequiredService<IOptions<FeatureManagementOptions>>())
                {
                    _telemetryPublisher = sp.GetService<ITelemetryPublisher>(),
                });

            services.AddSingleton<ISessionManager, EmptySessionManager>();

            services.AddScoped<IFeatureManagerSnapshot, FeatureManagerSnapshot>();

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
