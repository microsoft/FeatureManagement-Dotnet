// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Targeting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// A feature filter that can be used to activate feature flags for targeted audiences.
    /// </summary>
    [FilterAlias(Alias)]
    public class ContextualTargetingFilter : IContextualFeatureFilter<ITargetingContext>
    {
        private const string Alias = "Microsoft.Targeting";
        private readonly TargetingEvaluationOptions _options;

        /// <summary>
        /// Creates a targeting contextual feature filter.
        /// </summary>
        /// <param name="options">Options controlling the behavior of the targeting evaluation performed by the filter.</param>
        public ContextualTargetingFilter(IOptions<TargetingEvaluationOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Performs a targeting evaluation using the provided <see cref="TargetingContext"/> to determine if a feature flag should be enabled.
        /// </summary>
        /// <param name="context">The feature evaluation context.</param>
        /// <param name="targetingContext">The targeting context to use during targeting evaluation.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if either <paramref name="context"/> or <paramref name="targetingContext"/> is null.</exception>
        /// <returns>True if the feature flag is enabled, false otherwise.</returns>
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, ITargetingContext targetingContext, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (targetingContext == null)
            {
                throw new ArgumentNullException(nameof(targetingContext));
            }

            TargetingFilterSettings settings = context.Parameters.Get<TargetingFilterSettings>() ?? new TargetingFilterSettings();

            return Task.FromResult(TargetingEvaluator.IsTargeted(targetingContext, settings, _options.IgnoreCase, context.FeatureFlagName));
        }
    }
}
