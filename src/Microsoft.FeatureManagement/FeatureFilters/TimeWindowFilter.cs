// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.FeatureFilters.Cron;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// A feature filter that can be used to activate a feature based on a singular or recurring time window.
    /// It supports activating the feature flag during a fixed time window, 
    /// and also allows for configuring recurring time window filters to activate the feature flag periodically.
    /// </summary>
    [FilterAlias(Alias)]
    public class TimeWindowFilter : IFeatureFilter, IFilterParametersBinder
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
        /// Binds configuration representing filter parameters to <see cref="TimeWindowFilterSettings"/>.
        /// </summary>
        /// <param name="filterParameters">The configuration representing filter parameters that should be bound to <see cref="TimeWindowFilterSettings"/>.</param>
        /// <returns><see cref="TimeWindowFilterSettings"/> that can later be used in feature evaluation.</returns>
        public object BindParameters(IConfiguration filterParameters)
        {
            return filterParameters.Get<TimeWindowFilterSettings>() ?? new TimeWindowFilterSettings();
        }

        /// <summary>
        /// Evaluates whether a feature is enabled based on a configurable time window.
        /// </summary>
        /// <param name="context">The feature evaluation context.</param>
        /// <returns>True if the feature is enabled, false otherwise.</returns>
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            //
            // Check if prebound settings available, otherwise bind from parameters.
            TimeWindowFilterSettings settings = (TimeWindowFilterSettings)context.Settings ?? (TimeWindowFilterSettings)BindParameters(context.Parameters);

            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (!settings.Start.HasValue && !settings.End.HasValue && (settings.Filters == null || !settings.Filters.Any()))
            {
                _logger.LogWarning($"The '{Alias}' feature filter is not valid for feature '{context.FeatureName}'. It must specify at least one of '{nameof(settings.Start)}', '{nameof(settings.End)}' or '{nameof(settings.Filters)}'.");

                return Task.FromResult(false);
            }

            bool enabled = (!settings.Start.HasValue || now >= settings.Start.Value) && (!settings.End.HasValue || now < settings.End.Value);

            if (!enabled)
            {
                return Task.FromResult(false);
            }

            //
            // If any recurring time window filter is specified, to activate the feature flag, the current time also needs to be within at least one of the recurring time windows.
            if (settings.Filters != null && settings.Filters.Any())
            {
                enabled = false;

                TimeSpan utcOffsetForCron = new TimeSpan(0, 0, 0); // By default, the UTC offset is UTC+00:00.
                utcOffsetForCron = settings.Start.HasValue
                                    ? settings.Start.Value.Offset
                                    : settings.End.HasValue 
                                        ? settings.End.Value.Offset
                                        : utcOffsetForCron;

                DateTimeOffset nowForCron = now + utcOffsetForCron;

                foreach (string expression in settings.Filters)
                {
                    CronExpression cronExpression = CronExpression.Parse(expression);
                    if (cronExpression.IsSatisfiedBy(nowForCron))
                    {
                        enabled = true;

                        break;
                    }
                }
            }

            return Task.FromResult(enabled);
        }
    }
}
