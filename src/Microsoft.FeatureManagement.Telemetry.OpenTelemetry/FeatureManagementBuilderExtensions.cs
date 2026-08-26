// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement.Telemetry.OpenTelemetry;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Extensions used to add feature management functionality.
    /// </summary>
    public static class FeatureManagementBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="OpenTelemetryEventPublisher"/>, using <see cref="OpenTelemetryHostedService"/>, and the <see cref="TargetingActivityProcessor"/> to the feature management builder.
        /// </summary>
        /// <remarks>
        /// To have <see cref="TargetingActivityProcessor"/> enrich exported traces/spans with targeting information, register it with the app's <c>TracerProviderBuilder</c>:
        /// <c>tracerProviderBuilder.AddProcessor(serviceProvider.GetRequiredService&lt;TargetingActivityProcessor&gt;())</c>.
        /// This is only necessary if the app also wants to export the raw <see cref="System.Diagnostics.Activity"/>/span data; it is not required for the OpenTelemetry log-based <c>FeatureEvaluation</c> custom event.
        /// </remarks>
        /// <param name="builder">The feature management builder.</param>
        /// <returns>The feature management builder.</returns>
        public static IFeatureManagementBuilder AddOpenTelemetry(this IFeatureManagementBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (builder.Services == null)
            {
                throw new ArgumentException($"The provided builder's services must not be null.", nameof(builder));
            }

            builder.Services.AddSingleton<OpenTelemetryEventPublisher>();

            builder.Services.AddSingleton<TargetingActivityProcessor>();

            if (!builder.Services.Any((ServiceDescriptor d) => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(OpenTelemetryHostedService)))
            {
                builder.Services.Insert(0, ServiceDescriptor.Singleton<IHostedService, OpenTelemetryHostedService>());
            }

            return builder;
        }
    }
}
