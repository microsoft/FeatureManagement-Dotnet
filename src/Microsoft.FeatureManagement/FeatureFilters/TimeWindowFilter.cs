// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// A feature filter that can be used to activate a feature based on a time window.
    /// </summary>
    [FilterAlias(Alias)]
    public class TimeWindowFilter : IFeatureFilter
    {
        private const string Alias = "Microsoft.TimeWindow";
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a time window based feature filter.
        /// </summary>
        /// <param name="loggerFactory">A logger factory for creating loggers.</param>
        public TimeWindowFilter(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TimeWindowFilter>();
        }

        /// <summary>
        /// Evaluates whether a feature is enabled based on a configurable time window.
        /// </summary>
        /// <param name="context">The feature evaluation context.</param>
        /// <returns>True if the feature is enabled, false otherwise.</returns>
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            TimeWindowFilterSettings settings = context.Parameters.Get<TimeWindowFilterSettings>() ?? new TimeWindowFilterSettings();

            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (!settings.Start.HasValue && !settings.End.HasValue)
            {
                _logger.LogWarning($"The '{Alias}' feature filter is not valid for feature '{context.FeatureName}'. It must have have specify either '{nameof(settings.Start)}', '{nameof(settings.End)}', or both.");

                return Task.FromResult(false);
            }

            return Task.FromResult((!settings.Start.HasValue || now >= settings.Start.Value) && (!settings.End.HasValue || now < settings.End.Value));
        }
    }
}
