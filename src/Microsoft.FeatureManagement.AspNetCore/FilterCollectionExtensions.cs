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
        /// <returns></returns>
        public static IFilterMetadata AddForFeature<TFilterType>(this FilterCollection filters, string feature) where TFilterType : IAsyncActionFilter
        {
            IFilterMetadata filterMetadata = null;

            filters.Add(new FeatureGatedAsyncActionFilter<TFilterType>(feature));

            return filterMetadata;
        }
    }
}
