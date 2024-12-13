// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement.Telemetry.ApplicationInsights;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Extensions used to add feature management functionality.
    /// </summary>
    public static class FeatureManagementBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="TargetingTelemetryInitializer"/> and the <see cref="ApplicationInsightsEventPublisher"/> using <see cref="ApplicationInsightsHostedService"/> to the feature management builder.
        /// </summary>
        /// <param name="builder">The feature management builder.</param>
        /// <returns>The feature management builder.</returns>
        public static IFeatureManagementBuilder AddApplicationInsightsTelemetry(this IFeatureManagementBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (builder.Services == null)
            {
                throw new ArgumentException($"The provided builder's services must not be null.", nameof(builder));
            }

            builder.Services.AddSingleton<ITelemetryInitializer, TargetingTelemetryInitializer>();

            builder.Services.AddSingleton<ApplicationInsightsEventPublisher>();

            if (!builder.Services.Any((ServiceDescriptor d) => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(ApplicationInsightsHostedService)))
            {
                builder.Services.Insert(0, ServiceDescriptor.Singleton<IHostedService, ApplicationInsightsHostedService>());
            }

            return builder;
        }
    }
}
