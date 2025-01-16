using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.AspNetCore
{
    /// <summary>
    /// Extension methods that provide feature management integration for ASP.NET Core endpoint building.
    /// </summary>
    public static class FeatureFlagsEndpointFilterExtensions
    {
        /// <summary>
        /// Adds a feature flag filter to the endpoint that controls access based on feature state.
        /// </summary>
        /// <param name="builder">The endpoint convention builder.</param>
        /// <param name="featureName">The name of the feature flag to evaluate.</param>
        /// <returns>The endpoint convention builder for chaining.</returns>
        /// <remarks>
        /// This extension method enables feature flag control over endpoint access. When the feature is disabled,
        /// requests to the endpoint will return a 404 Not Found response. The targeting context is obtained
        /// from the ITargetingContextAccessor registered in the service collection.
        /// </remarks>
        /// <example>
        /// <code>
        /// endpoints.MapGet("/api/feature", () => "Feature Enabled")
        ///     .WithFeatureGate("MyFeature");
        /// </code>
        /// </example>
        public static IEndpointConventionBuilder WithFeatureGate(this IEndpointConventionBuilder builder, string featureName)
        {
            return builder.AddEndpointFilter(new FeatureFlagsEndpointFilter(featureName));
        }
    }
}
