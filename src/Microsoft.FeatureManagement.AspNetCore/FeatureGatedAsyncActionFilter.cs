// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A place holder MVC filter that is used to dynamically activate a filter based on whether a feature is enabled.
    /// </summary>
    /// <typeparam name="T">The filter that will be used instead of this placeholder.</typeparam>
    class FeatureGatedAsyncActionFilter<T> : IAsyncActionFilter where T : IAsyncActionFilter
    {
        public FeatureGatedAsyncActionFilter(string featureName)
        {
            if (string.IsNullOrEmpty(featureName))
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            FeatureName = featureName;
        }

        public string FeatureName { get; }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            IFeatureManager featureManager = context.HttpContext.RequestServices.GetRequiredService<IFeatureManagerSnapshot>();

            if (await featureManager.IsEnabledAsync(FeatureName).ConfigureAwait(false))
            {
                IAsyncActionFilter filter = ActivatorUtilities.CreateInstance<T>(context.HttpContext.RequestServices);

                await filter.OnActionExecutionAsync(context, next).ConfigureAwait(false);
            }
            else
            {
                await next().ConfigureAwait(false);
            }
        }
    }
}
