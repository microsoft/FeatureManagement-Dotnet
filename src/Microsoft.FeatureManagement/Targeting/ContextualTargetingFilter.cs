// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

        private StringComparison ComparisonType => _options.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        private StringComparer ComparerType => _options.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

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

            if (!TryValidateSettings(settings, out string paramName, out string message))
            {
                throw new ArgumentException(message, paramName);
            }

            if (settings.Audience.Exclusion != null)
            {
                //
                // Check if the user is in the exclusion directly
                if (targetingContext.UserId != null &&
                    settings.Audience.Exclusion.Users != null &&
                    settings.Audience.Exclusion.Users.Any(user => targetingContext.UserId.Equals(user, ComparisonType)))
                {
                    return Task.FromResult(false);
                }

                //
                // Check if the user is in a group within exclusion
                if (targetingContext.Groups != null &&
                    settings.Audience.Exclusion.Groups != null &&
                    settings.Audience.Exclusion.Groups.Any(group => targetingContext.Groups.Contains(group, ComparerType)))
                {
                    return Task.FromResult(false);
                }
            }

            //
            // Check if the user is being targeted directly
            if (targetingContext.UserId != null &&
                settings.Audience.Users != null &&
                settings.Audience.Users.Any(user => targetingContext.UserId.Equals(user, ComparisonType)))
            {
                return Task.FromResult(true);
            }

            //
            // Check if the user is in a group that is being targeted
            if (targetingContext.Groups != null &&
                settings.Audience.Groups != null)
            {
                foreach (string group in targetingContext.Groups)
                {
                    GroupRollout groupRollout = settings.Audience.Groups.FirstOrDefault(g => g.Name.Equals(group, ComparisonType));

                    if (groupRollout != null)
                    {
                        string audienceContextId = $"{targetingContext.UserId}\n{context.FeatureName}\n{group}";

                        if (IsTargeted(audienceContextId, groupRollout.RolloutPercentage))
                        {
                            return Task.FromResult(true);
                        }
                    }
                }
            }

            //
            // Check if the user is being targeted by a default rollout percentage
            string defaultContextId = $"{targetingContext.UserId}\n{context.FeatureName}";

            return Task.FromResult(IsTargeted(defaultContextId, settings.Audience.DefaultRolloutPercentage));
        }


        /// <summary>
        /// Determines if a given context id should be targeted based off the provided percentage
        /// </summary>
        /// <param name="contextId">A context identifier that determines what the percentage is applicable for</param>
        /// <param name="percentage">The total percentage of possible context identifiers that should be targeted</param>
        /// <returns>A boolean representing if the context identifier should be targeted</returns>
        private bool IsTargeted(string contextId, double percentage)
        {
            //
            // Handle edge case of exact 100 bucket
            if (percentage == 100)
            {
                return true;
            }

            byte[] hash;

            using (HashAlgorithm hashAlgorithm = SHA256.Create())
            {
                hash = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(contextId));
            }

            //
            // Use first 4 bytes for percentage calculation
            // Cryptographic hashing algorithms ensure adequate entropy across hash
            uint contextMarker = BitConverter.ToUInt32(hash, 0);

            double contextPercentage = (contextMarker / (double)uint.MaxValue) * 100;

            return contextPercentage < percentage;
        }

        /// <summary>
        /// Performs validation of targeting settings.
        /// </summary>
        /// <param name="settings">The settings to validate.</param>
        /// <param name="paramName">The name of the invalid setting, if any.</param>
        /// <param name="reason">The reason that the setting is invalid.</param>
        /// <returns>True if the provided settings are valid. False if the provided settings are invalid.</returns>
        private bool TryValidateSettings(TargetingFilterSettings settings, out string paramName, out string reason)
        {
            const string OutOfRange = "The value is out of the accepted range.";

            const string RequiredParameter = "Value cannot be null.";

            paramName = null;

            reason = null;

            if (settings.Audience == null)
            {
                paramName = nameof(settings.Audience);

                reason = RequiredParameter;

                return false;
            }

            if (settings.Audience.DefaultRolloutPercentage < 0 || settings.Audience.DefaultRolloutPercentage > 100)
            {
                paramName = $"{settings.Audience}.{settings.Audience.DefaultRolloutPercentage}";

                reason = OutOfRange;

                return false;
            }

            if (settings.Audience.Groups != null)
            {
                int index = 0;

                foreach (GroupRollout groupRollout in settings.Audience.Groups)
                {
                    index++;

                    if (groupRollout.RolloutPercentage < 0 || groupRollout.RolloutPercentage > 100)
                    {
                        //
                        // Audience.Groups[1].RolloutPercentage
                        paramName = $"{settings.Audience}.{settings.Audience.Groups}[{index}].{groupRollout.RolloutPercentage}";

                        reason = OutOfRange;

                        return false;
                    }
                }
            }

            return true;
        }
    }
}
