// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement.Telemetry.ApplicationInsights;
using OpenTelemetry.Trace;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Extensions used to add feature management functionality.
    /// </summary>
    public static class FeatureManagementBuilderExtensions
    {
        /// <summary>
        /// Adds an <see cref="ITargetingContextAccessor"/> to be used for targeting and registers the targeting filter to the feature management system.
        /// </summary>
        /// <param name="builder">The <see cref="IFeatureManagementBuilder"/> used to customize feature management functionality.</param>
        /// <returns>A <see cref="IFeatureManagementBuilder"/> that can be used to customize feature management functionality.</returns>
        public static TracerProviderBuilder AddEvaluationEventExporter<T>(this TracerProviderBuilder builder) where T : ITargetingContextAccessor
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var deferredBuilder = builder as IDeferredTracerProviderBuilder;
            if (deferredBuilder == null)
            {
                throw new InvalidOperationException("The provided TracerProviderBuilder does not implement IDeferredTracerProviderBuilder.");
            }

            return deferredBuilder.Configure((sp, builder) =>
            {

                builder.AddSource("Microsoft.FeatureManagement");
                //builder.AddProcessor(new EvaluationEventExporter(sp.GetRequiredService<TelemetryClient>()));
            });
        }
    }
}
