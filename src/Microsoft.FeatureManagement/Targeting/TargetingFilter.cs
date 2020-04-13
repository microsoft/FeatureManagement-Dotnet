// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// A feature filter that can be used to activate features for targeted audiences.
    /// </summary>
    [FilterAlias(Alias)]
    public class TargetingFilter : IFeatureFilter
    {
        private const string Alias = "Microsoft.Targeting";
        private readonly ITargetingContextAccessor _contextAccessor;
        private readonly IContextualFeatureFilter<ITargetingContext> _contextualFilter;

        /// <summary>
        /// Creates a targeting feature filter.
        /// </summary>
        /// <param name="contextAccessor">An accessor used to acquire the targeting context for use in feature evaluation.</param>
        /// <param name="loggerFactory">A logger factory for creating loggers.</param>
        public TargetingFilter(ITargetingContextAccessor contextAccessor, ILoggerFactory loggerFactory)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _contextualFilter = new ContextualTargetingFilter(loggerFactory);
        }

        /// <summary>
        /// Performs a targeting evaluation using the current <see cref="TargetingContext"/> to determine if a feature should be enabled.
        /// </summary>
        /// <param name="context">The feature evaluation context.</param>
        /// <returns>True if the feature is enabled, false otherwise.</returns>
        public async Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            //
            // Acquire targeting context via accessor
            TargetingContext targetingContext = await _contextAccessor.GetContextAsync().ConfigureAwait(false);

            //
            // Utilize contextual filter for targeting evaluation
            return await _contextualFilter.EvaluateAsync(context, targetingContext).ConfigureAwait(false);
        }
    }
}
