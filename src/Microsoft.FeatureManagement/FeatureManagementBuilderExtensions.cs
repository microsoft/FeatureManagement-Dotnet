// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FeatureManagement.FeatureFilters;

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
        public static IFeatureManagementBuilder WithTargeting<T>(this IFeatureManagementBuilder builder) where T : ITargetingContextAccessor
        {
            builder.Services.TryAddSingleton(typeof(ITargetingContextAccessor), typeof(T));

            builder.AddFeatureFilter<TargetingFilter>();

            return builder;
        }
    }
}
