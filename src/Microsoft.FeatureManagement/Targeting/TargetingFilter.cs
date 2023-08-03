// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// A feature filter that can be used to activate features for targeted audiences.
    /// </summary>
    [FilterAlias(Alias)]
    public class TargetingFilter : IFeatureFilter, IFilterParametersBinder
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
        /// Binds configuration representing filter parameters to <see cref="TargetingFilterSettings"/>.
        /// </summary>
        /// <param name="filterParameters">The configuration representing filter parameters that should be bound to <see cref="TargetingFilterSettings"/>.</param>
        /// <returns><see cref="TargetingFilterSettings"/> that can later be used in targeting.</returns>
        public object BindParameters(IConfiguration filterParameters)
        {
            return filterParameters.Get<TargetingFilterSettings>() ?? new TargetingFilterSettings();
        }

        /// <summary>
        /// Performs a targeting evaluation using the current <see cref="TargetingContext"/> to determine if a feature should be enabled.
        /// </summary>
        /// <param name="context">The feature evaluation context.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is null.</exception>
        /// <returns>True if the feature is enabled, false otherwise.</returns>
        public async Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            //
            // Acquire targeting context via accessor
            TargetingContext targetingContext = await _contextAccessor.GetContextAsync().ConfigureAwait(false);

            //
            // Ensure targeting can be performed
            if (targetingContext == null)
            {
                _logger.LogWarning("No targeting context available for targeting evaluation.");

                return false;
            }

            //
            // Utilize contextual filter for targeting evaluation
            return await _contextualFilter.EvaluateAsync(context, targetingContext).ConfigureAwait(false);
        }
    }
}
