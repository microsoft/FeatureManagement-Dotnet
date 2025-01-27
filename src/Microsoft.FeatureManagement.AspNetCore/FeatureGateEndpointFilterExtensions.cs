// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.FeatureManagement.AspNetCore
{
    /// <summary>
    /// Extension methods that provide feature management integration for ASP.NET Core endpoint building.
    /// </summary>
    public static class FeatureGateEndpointFilterExtensions
    {
        /// <summary>
        /// Adds a feature flag filter to the endpoint that controls access based on feature state.
        /// </summary>
        /// <param name="builder">The endpoint convention builder.</param>
        /// <param name="feature">The name of the feature flag to evaluate.</param>
        /// <returns>The endpoint convention builder for chaining.</returns>
        /// <remarks>
        /// This extension method enables feature flag control over endpoint access. When the feature is disabled,
        /// requests to the endpoint will return a 404 Not Found response.
        /// </remarks>
        /// <example>
        /// <code>
        /// endpoints.MapGet("/api/feature", () => "Feature Enabled")
        ///     .WithFeatureGate("MyFeature");
        /// </code>
        /// </example>
        public static IEndpointConventionBuilder WithFeatureGate(this IEndpointConventionBuilder builder, string feature)
        {
            return builder.AddEndpointFilter(new FeatureGateEndpointFilter(feature));
        }

        /// <summary>
        /// Adds a feature flag filter to the endpoint that controls access based on multiple feature states.
        /// All features must be enabled for access to be granted.
        /// </summary>
        /// <param name="builder">The endpoint convention builder.</param>
        /// <param name="features">The collection of feature flags to evaluate.</param>
        /// <returns>The endpoint convention builder for chaining.</returns>
        /// <remarks>
        /// When multiple features are specified, all features must be enabled for access to be granted.
        /// </remarks>
        public static IEndpointConventionBuilder WithFeatureGate(this IEndpointConventionBuilder builder, params string[] features)
        {
            return builder.AddEndpointFilter(new FeatureGateEndpointFilter(features));
        }

        /// <summary>
        /// Adds a feature flag filter to the endpoint with specified requirement type for multiple features.
        /// </summary>
        /// <param name="builder">The endpoint convention builder.</param>
        /// <param name="requirementType">The type of requirement for feature evaluation (All or Any).</param>
        /// <param name="features">The collection of feature flags to evaluate.</param>
        /// <returns>The endpoint convention builder for chaining.</returns>
        /// <remarks>
        /// Use RequirementType.All to require all features to be enabled.
        /// Use RequirementType.Any to require at least one feature to be enabled.
        /// </remarks>
        public static IEndpointConventionBuilder WithFeatureGate(this IEndpointConventionBuilder builder, RequirementType requirementType, params string[] features)
        {
            return builder.AddEndpointFilter(new FeatureGateEndpointFilter(requirementType, features));
        }

        /// <summary>
        /// Adds a feature flag filter to the endpoint with negation capability for multiple features.
        /// </summary>
        /// <param name="builder">The endpoint convention builder.</param>
        /// <param name="negate">Whether to negate the feature evaluation result.</param>
        /// <param name="features">The collection of feature flags to evaluate.</param>
        /// <returns>The endpoint convention builder for chaining.</returns>
        /// <remarks>
        /// When negate is true, access is granted when features are disabled rather than enabled.
        /// </remarks>
        public static IEndpointConventionBuilder WithFeatureGate(this IEndpointConventionBuilder builder, bool negate, params string[] features)
        {
            return builder.AddEndpointFilter(new FeatureGateEndpointFilter(RequirementType.All, negate, features));
        }

        /// <summary>
        /// Adds a feature flag filter to the endpoint with full control over requirement type and negation.
        /// </summary>
        /// <param name="builder">The endpoint convention builder.</param>
        /// <param name="requirementType">The type of requirement for feature evaluation (All or Any).</param>
        /// <param name="negate">Whether to negate the feature evaluation result.</param>
        /// <param name="features">The collection of feature flags to evaluate.</param>
        /// <returns>The endpoint convention builder for chaining.</returns>
        /// <remarks>
        /// This method provides complete control over feature evaluation behavior:
        /// - Use requirementType to specify if all or any features must be enabled
        /// - Use negate to invert the evaluation result
        /// </remarks>
        public static IEndpointConventionBuilder WithFeatureGate(this IEndpointConventionBuilder builder, RequirementType requirementType, bool negate, params string[] features)
        {
            return builder.AddEndpointFilter(new FeatureGateEndpointFilter(requirementType, negate, features));
        }
    }
}
