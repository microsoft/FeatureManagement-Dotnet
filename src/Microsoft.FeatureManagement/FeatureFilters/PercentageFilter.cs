﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.Utils;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// A feature filter that can be used to activate a feature based on a random percentage.
    /// </summary>
    [FilterAlias(Alias)]
    public class PercentageFilter : IFeatureFilter, IFilterParametersBinder
    {
        private const string Alias = "Microsoft.Percentage";
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a percentage based feature filter.
        /// </summary>
        /// <param name="loggerFactory">A logger factory for creating loggers.</param>
        public PercentageFilter(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<PercentageFilter>();
        }

        /// <summary>
        /// Binds configuration representing filter parameters to <see cref="PercentageFilterSettings"/>.
        /// </summary>
        /// <param name="filterParameters">The configuration representing filter parameters that should be bound to <see cref="PercentageFilterSettings"/>.</param>
        /// <returns><see cref="PercentageFilterSettings"/> that can later be used in feature evaluation.</returns>
        public object BindParameters(IConfiguration filterParameters)
        {
            return filterParameters.Get<PercentageFilterSettings>() ?? new PercentageFilterSettings();
        }

        /// <summary>
        /// Performs a percentage based evaluation to determine whether a feature flag is enabled.
        /// </summary>
        /// <param name="context">The feature evaluation context.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>True if the feature flag is enabled, false otherwise.</returns>
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, CancellationToken cancellationToken)
        {
            PercentageFilterSettings settings = (PercentageFilterSettings)context.Settings;

            bool result = true;

            if (settings.Value < 0)
            {
                _logger.LogWarning($"The '{Alias}' feature filter does not have a valid '{nameof(settings.Value)}' value for the feature flag '{context.FeatureFlagName}'");

                result = false;
            }

            if (result)
            {
                result = (RandomGenerator.NextDouble() * 100) < settings.Value;
            }

            return Task.FromResult(result);
        }
    }
}
