// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A place holder MVC filter that is used to dynamically activate a filter based on whether a feature flag is enabled.
    /// </summary>
    /// <typeparam name="T">The filter that will be used instead of this placeholder.</typeparam>
    class FeatureGatedAsyncActionFilter<T> : IAsyncActionFilter where T : IAsyncActionFilter
    {
        public FeatureGatedAsyncActionFilter(string featureFlagName)
        {
            if (string.IsNullOrEmpty(featureFlagName))
            {
                throw new ArgumentNullException(nameof(featureFlagName));
            }

            FeatureFlagName = featureFlagName;
        }

        public string FeatureFlagName { get; }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            IFeatureManager featureManager = context.HttpContext.RequestServices.GetRequiredService<IFeatureManagerSnapshot>();

            if (await featureManager.IsEnabledAsync(FeatureFlagName, context.HttpContext.RequestAborted).ConfigureAwait(false))
            {
                IServiceProvider serviceProvider = context.HttpContext.RequestServices.GetRequiredService<IServiceProvider>();

                IAsyncActionFilter filter = ActivatorUtilities.CreateInstance<T>(serviceProvider);

                await filter.OnActionExecutionAsync(context, next).ConfigureAwait(false);
            }
            else
            {
                await next().ConfigureAwait(false);
            }
        }
    }
}
