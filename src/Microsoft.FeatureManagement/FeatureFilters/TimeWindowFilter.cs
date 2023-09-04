// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.FeatureFilters.Crontab;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// A feature filter that can be used to activate a feature based on time window.
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
            DateTimeOffset? Start = filterParameters["Start"] != null ? filterParameters.GetValue<DateTimeOffset>("Start") : (DateTimeOffset?)null;
            DateTimeOffset? End = filterParameters["End"] != null ? filterParameters.GetValue<DateTimeOffset>("End") : (DateTimeOffset?)null;

            TimeWindowFilterSettings settings = new TimeWindowFilterSettings()
            {
                Start = Start,
                End = End
            };

            List<string> Filters = filterParameters.GetSection("Filters").Get<List<string>>();
            if (Filters != null)
            {
                foreach (string expression in Filters)
                {
                    CrontabExpression crontabExpression = CrontabExpression.Parse(expression);
                    settings.Filters.Add(crontabExpression);
                }
            }

            return settings;
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

            if (!settings.Start.HasValue && !settings.End.HasValue && settings.Filters.Count == 0)
            {
                _logger.LogWarning($"The '{Alias}' feature filter is not valid for feature '{context.FeatureName}'. It must specify at least one of '{nameof(settings.Start)}', '{nameof(settings.End)}' and '{nameof(settings.Filters)}'.");

                return Task.FromResult(false);
            }

            bool enabled = (!settings.Start.HasValue || now >= settings.Start.Value) && (!settings.End.HasValue || now < settings.End.Value);

            if (!enabled)
            {
                return Task.FromResult(false);
            }

            //
            // If any recurring time window filters is specified, to activate the feature flag, the current time also needs to be within at least one of the recurring time windows.
            if (settings.Filters.Count > 0)
            {
                enabled = false;

                TimeSpan utcOffsetForCrontab = new TimeSpan(0, 0, 0); // By default, the UTC offset is UTC+00:00.
                utcOffsetForCrontab = settings.Start.HasValue ? settings.Start.Value.Offset :
                                      settings.End.HasValue ? settings.End.Value.Offset :
                                      utcOffsetForCrontab;
                DateTimeOffset nowForCrontab = now + utcOffsetForCrontab;

                foreach (CrontabExpression crontabExpression in settings.Filters)
                {
                    if (crontabExpression.IsSatisfiedBy(nowForCrontab))
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
