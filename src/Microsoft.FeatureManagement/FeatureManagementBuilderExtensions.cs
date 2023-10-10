using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement.Telemetry;
using System;
using System.Collections.Generic;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Extensions used to add feature management functionality.
    /// </summary>
    public static class FeatureManagementBuilderExtensions
    {
        /// <summary>
        /// Adds a telemetry publisher to the feature management system.
        /// </summary>
        /// <param name="builder">The <see cref="IFeatureManagementBuilder"/> used to customize feature management functionality.</param>
        /// <returns>A <see cref="IFeatureManagementBuilder"/> that can be used to customize feature management functionality.</returns>
        public static IFeatureManagementBuilder AddTelemetryPublisher<T>(this IFeatureManagementBuilder builder) where T : ITelemetryPublisher
        {
            builder.AddTelemetryPublisher(sp => ActivatorUtilities.CreateInstance(sp, typeof(T)) as ITelemetryPublisher);

            return builder;
        }

        private static IFeatureManagementBuilder AddTelemetryPublisher(this IFeatureManagementBuilder builder, Func<IServiceProvider, ITelemetryPublisher> factory)
        {
            builder.Services.Configure<FeatureManagementOptions>(options =>
            {
                if (options.TelemetryPublisherFactories == null)
                {
                    options.TelemetryPublisherFactories = new List<Func<IServiceProvider, ITelemetryPublisher>>();
                }

                options.TelemetryPublisherFactories.Add(factory);
            });

            return builder;
        }
    }
}
