// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
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
    public class ContextualTargetingFilter : IContextualFeatureFilter<ITargetingContext>
    {
        private const string Alias = "Microsoft.Targeting";
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a targeting contextual feature filter.
        /// </summary>
        /// <param name="loggerFactory">A logger factory for creating loggers.</param>
        public ContextualTargetingFilter(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger<ContextualTargetingFilter>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Performs a targeting evaluation using the provided <see cref="TargetingContext"/> to determine if a feature should be enabled.
        /// </summary>
        /// <param name="context">The feature evaluation context.</param>
        /// <param name="targetingContext">The targeting context to use during targeting evaluation.</param>
        /// <returns>True if the feature is enabled, false otherwise.</returns>
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, ITargetingContext targetingContext)
        {
            TargetingFilterSettings settings = context.Parameters.Get<TargetingFilterSettings>() ?? new TargetingFilterSettings();

            //
            // No audience targeted, the feature will be off
            if (settings.Audience == null)
            {
                _logger.LogWarning($"The '{Alias} feature filter does not have an audience for the feature '{context.FeatureName}'.");

                return Task.FromResult(false);
            }

            //
            // Check if the user is being targeted directly
            if (targetingContext.UserId != null &&
                settings.Audience.Users != null &&
                settings.Audience.Users.Any(user => targetingContext.UserId.Equals(user)))
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
                    GroupRollout groupRollout = settings.Audience.Groups.FirstOrDefault(g => g.Name.Equals(group));

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
    }
}
