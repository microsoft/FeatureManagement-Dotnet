// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Targeting;
using System;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// A feature filter that can be used to activate features for targeted audiences.
    /// </summary>
    [FilterAlias(Alias)]
    public class ContextualTargetingFilter : IContextualFeatureFilter<ITargetingContext>, IFilterParametersBinder
    {
        private const string Alias = "Microsoft.Targeting";
        private readonly TargetingEvaluationOptions _options;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a targeting contextual feature filter.
        /// </summary>
        /// <param name="options">Options controlling the behavior of the targeting evaluation performed by the filter.</param>
        /// <param name="loggerFactory">A logger factory for creating loggers.</param>
        public ContextualTargetingFilter(IOptions<TargetingEvaluationOptions> options, ILoggerFactory loggerFactory)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = loggerFactory?.CreateLogger<ContextualTargetingFilter>() ?? throw new ArgumentNullException(nameof(loggerFactory));
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
        /// Performs a targeting evaluation using the provided <see cref="TargetingContext"/> to determine if a feature should be enabled.
        /// </summary>
        /// <param name="context">The feature evaluation context.</param>
        /// <param name="targetingContext">The targeting context to use during targeting evaluation.</param>
        /// <exception cref="ArgumentNullException">Thrown if either <paramref name="context"/> or <paramref name="targetingContext"/> is null.</exception>
        /// <returns>True if the feature is enabled, false otherwise.</returns>
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, ITargetingContext targetingContext)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (targetingContext == null)
            {
                throw new ArgumentNullException(nameof(targetingContext));
            }

            //
            // Check if prebound settings available, otherwise bind from parameters.
            TargetingFilterSettings settings = (TargetingFilterSettings)context.Settings ?? (TargetingFilterSettings)BindParameters(context.Parameters);

            return Task.FromResult(TargetingEvaluator.IsTargeted(targetingContext, settings, _options.IgnoreCase, context.FeatureName));
        }
    }
}
