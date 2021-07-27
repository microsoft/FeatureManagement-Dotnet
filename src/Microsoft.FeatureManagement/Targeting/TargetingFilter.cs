// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
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
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a targeting feature filter.
        /// </summary>
        /// <param name="options">Options controlling the behavior of the targeting evaluation performed by the filter.</param>
        /// <param name="contextAccessor">An accessor used to acquire the targeting context for use in feature evaluation.</param>
        /// <param name="loggerFactory">A logger factory for creating loggers.</param>
        public TargetingFilter(IOptions<TargetingEvaluationOptions> options, ITargetingContextAccessor contextAccessor, ILoggerFactory loggerFactory)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _contextualFilter = new ContextualTargetingFilter(options, loggerFactory);
            _logger = loggerFactory?.CreateLogger<TargetingFilter>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Performs a targeting evaluation using the current <see cref="TargetingContext"/> to determine if a feature should be enabled.
        /// </summary>
        /// <param name="context">The feature evaluation context.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is null.</exception>
        /// <returns>True if the feature is enabled, false otherwise.</returns>
        public async Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            //
            // Acquire targeting context via accessor
            TargetingContext targetingContext = await _contextAccessor.GetContextAsync(cancellationToken).ConfigureAwait(false);

            //
            // Ensure targeting can be performed
            if (targetingContext == null)
            {
                _logger.LogWarning("No targeting context available for targeting evaluation.");

                return false;
            }

            //
            // Utilize contextual filter for targeting evaluation
            return await _contextualFilter.EvaluateAsync(context, targetingContext, cancellationToken).ConfigureAwait(false);
        }
    }
}
