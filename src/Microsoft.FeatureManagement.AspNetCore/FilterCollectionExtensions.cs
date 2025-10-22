// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides integration points for feature management with MVC Filters.
    /// </summary>
    public static class FilterCollectionExtensions
    {
        /// <summary>
        /// Adds an MVC filter that will only activate during a request if the specified feature is enabled.
        /// </summary>
        /// <typeparam name="TFilterType">The MVC filter to add and use if the feature is enabled.</typeparam>
        /// <param name="filters">The filter collection to add to.</param>
        /// <param name="feature">The feature that will need to enabled to trigger the execution of the MVC filter.</param>
        /// <returns>The reference to the added filter metadata.</returns>
        public static IFilterMetadata AddForFeature<TFilterType>(this FilterCollection filters, string feature) where TFilterType : IAsyncActionFilter
        {
            IFilterMetadata filterMetadata = new FeatureGatedAsyncActionFilter<TFilterType>(RequirementType.Any, false, feature);

            filters.Add(filterMetadata);

            return filterMetadata;
        }

        /// <summary>
        /// Adds an MVC filter that will only activate during a request if the specified feature is enabled.
        /// </summary>
        /// <typeparam name="TFilterType">The MVC filter to add and use if the feature is enabled.</typeparam>
        /// <param name="filters">The filter collection to add to.</param>
        /// <param name="features">The features that control whether the MVC filter executes.</param>
        /// <returns>The reference to the added filter metadata.</returns>
        public static IFilterMetadata AddForFeature<TFilterType>(this FilterCollection filters, params string[] features) where TFilterType : IAsyncActionFilter
        {
            IFilterMetadata filterMetadata = new FeatureGatedAsyncActionFilter<TFilterType>(RequirementType.Any, false, features);

            filters.Add(filterMetadata);

            return filterMetadata;
        }

        /// <summary>
        /// Adds an MVC filter that will only activate during a request if the specified features are enabled based on the provided requirement type.
        /// </summary>
        /// <typeparam name="TFilterType">The MVC filter to add and use if the features condition is satisfied.</typeparam>
        /// <param name="filters">The filter collection to add to.</param>
        /// <param name="requirementType">Specifies whether all or any of the provided features should be enabled.</param>
        /// <param name="features">The features that control whether the MVC filter executes.</param>
        /// <returns>The reference to the added filter metadata.</returns>
        public static IFilterMetadata AddForFeature<TFilterType>(this FilterCollection filters, RequirementType requirementType, params string[] features) where TFilterType : IAsyncActionFilter
        {
            IFilterMetadata filterMetadata = new FeatureGatedAsyncActionFilter<TFilterType>(requirementType, false, features);

            filters.Add(filterMetadata);

            return filterMetadata;
        }

        /// <summary>
        /// Adds an MVC filter that will only activate during a request if the specified features are enabled based on the provided requirement type and negation flag.
        /// </summary>
        /// <typeparam name="TFilterType">The MVC filter to add and use if the features condition is satisfied.</typeparam>
        /// <param name="filters">The filter collection to add to.</param>
        /// <param name="requirementType">Specifies whether all or any of the provided features should be enabled.</param>
        /// <param name="negate">Whether to negate the evaluation result for the provided features set.</param>
        /// <param name="features">The features that control whether the MVC filter executes.</param>
        /// <returns>The reference to the added filter metadata.</returns>
        public static IFilterMetadata AddForFeature<TFilterType>(this FilterCollection filters, RequirementType requirementType, bool negate, params string[] features) where TFilterType : IAsyncActionFilter
        {
            IFilterMetadata filterMetadata = new FeatureGatedAsyncActionFilter<TFilterType>(requirementType, negate, features);

            filters.Add(filterMetadata);

            return filterMetadata;
        }
    }
}
