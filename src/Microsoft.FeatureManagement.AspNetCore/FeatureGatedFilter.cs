// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A place holder MVC filter that is used to dynamically activate a filter based on whether a feature is enabled.
    /// </summary>
    /// <typeparam name="T">The filter that will be used instead of this placeholder.</typeparam>
    class FeatureGatedFilter<T> : IFilterFactory where T : IFilterMetadata
    {
        public FeatureGatedFilter(string featureName)
        {
            if (string.IsNullOrEmpty(featureName))
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            FeatureName = featureName;
        }

        public string FeatureName { get; }

        public bool IsReusable => false;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            IFeatureManager featureManager = serviceProvider.GetRequiredService<IFeatureManagerSnapshot>();

            if (featureManager.IsEnabled(FeatureName))
            {
                return (IFilterMetadata)ActivatorUtilities.CreateInstance(serviceProvider, typeof(T));
            }
            else
            {
                //
                // TODO check if null
                return new DisabledFeatureFilter(FeatureName);
            }
        }
    }
}
