// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A place holder MVC filter that is used to dynamically activate a filter based on whether a feature (or set of features) is enabled.
    /// </summary>
    /// <typeparam name="T">The filter that will be used instead of this placeholder.</typeparam>
    class FeatureGatedAsyncActionFilter<T> : IAsyncActionFilter where T : IAsyncActionFilter
    {
        /// <summary>
        /// Creates a feature gated filter for multiple features with a specified requirement type and ability to negate the evaluation.
        /// </summary>
        /// <param name="requirementType">Specifies whether all or any of the provided features should be enabled.</param>
        /// <param name="negate">Whether to negate the evaluation result.</param>
        /// <param name="features">The features that control whether the wrapped filter executes.</param>
        public FeatureGatedAsyncActionFilter(RequirementType requirementType, bool negate, params string[] features)
        {
            if (features == null || features.Length == 0)
            {
                throw new ArgumentNullException(nameof(features));
            }

            Features = features;
            RequirementType = requirementType;
            Negate = negate;
        }

        /// <summary>
        /// The set of features that gate the wrapped filter.
        /// </summary>
        public IEnumerable<string> Features { get; }

        /// <summary>
        /// Controls whether any or all features in <see cref="Features"/> should be enabled to allow the wrapped filter to execute.
        /// </summary>
        public RequirementType RequirementType { get; }

        /// <summary>
        /// Negates the evaluation for whether or not the wrapped filter should execute.
        /// </summary>
        public bool Negate { get; }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            IFeatureManagerSnapshot featureManager = context.HttpContext.RequestServices.GetRequiredService<IFeatureManagerSnapshot>();

            bool enabled;

            // Enabled state is determined by either 'any' or 'all' features being enabled.
            if (RequirementType == RequirementType.All)
            {
                enabled = await Features.All(async f => await featureManager.IsEnabledAsync(f).ConfigureAwait(false));
            }
            else
            {
                enabled = await Features.Any(async f => await featureManager.IsEnabledAsync(f).ConfigureAwait(false));
            }

            if (Negate)
            {
                enabled = !enabled;
            }

            if (enabled)
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
