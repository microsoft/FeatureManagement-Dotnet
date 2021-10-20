// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.FeatureManagement.Targeting
{
    static class TargetingEvaluator
    {
        private static StringComparison GetComparisonType(bool ignoreCase) =>
            ignoreCase ?
                StringComparison.OrdinalIgnoreCase :
                StringComparison.Ordinal;

        public static bool IsTargeted(TargetingFilterSettings settings, ITargetingContext targetingContext, bool ignoreCase, string hint)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (targetingContext == null)
            {
                throw new ArgumentNullException(nameof(targetingContext));
            }

            if (!TryValidateSettings(settings, out string paramName, out string reason))
            {
                throw new ArgumentException(reason, paramName);
            }

            //
            // Check if the user is being targeted directly
            if (targetingContext.UserId != null &&
                settings.Audience.Users != null &&
                settings.Audience.Users.Any(user => targetingContext.UserId.Equals(user, GetComparisonType(ignoreCase))))
            {
                return true;
            }

            string userId = ignoreCase ?
                targetingContext.UserId.ToLower() :
                targetingContext.UserId;

            //
            // Check if the user is in a group that is being targeted
            if (targetingContext.Groups != null &&
                settings.Audience.Groups != null)
            {
                IEnumerable<string> groups = ignoreCase ?
                    targetingContext.Groups.Select(g => g.ToLower()) :
                    targetingContext.Groups;

                foreach (string group in groups)
                {
                    GroupRollout groupRollout = settings.Audience.Groups.FirstOrDefault(g => g.Name.Equals(group, GetComparisonType(ignoreCase)));

                    if (groupRollout != null)
                    {
                        string audienceContextId = $"{userId}\n{hint}\n{group}";

                        if (IsTargeted(audienceContextId, groupRollout.RolloutPercentage))
                        {
                            return true;
                        }
                    }
                }
            }

            //
            // Check if the user is being targeted by a default rollout percentage
            string defaultContextId = $"{userId}\n{hint}";

            return IsTargeted(defaultContextId, settings.Audience.DefaultRolloutPercentage);
        }

        /// <summary>
        /// Performs validation of targeting settings.
        /// </summary>
        /// <param name="targetingSettings">The settings to validate.</param>
        /// <param name="paramName">The name of the invalid setting, if any.</param>
        /// <param name="reason">The reason that the setting is invalid.</param>
        /// <returns>True if the provided settings are valid. False if the provided settings are invalid.</returns>
        public static bool TryValidateSettings(TargetingFilterSettings targetingSettings, out string paramName, out string reason)
        {
            const string OutOfRange = "The value is out of the accepted range.";

            const string RequiredParameter = "Value cannot be null.";

            paramName = null;

            reason = null;

            if (targetingSettings == null)
            {
                paramName = nameof(FeatureFilterConfiguration.Parameters);

                reason = RequiredParameter;

                return false;
            }

            if (targetingSettings.Audience == null)
            {
                paramName = nameof(targetingSettings.Audience);

                reason = RequiredParameter;

                return false;
            }

            if (targetingSettings.Audience.DefaultRolloutPercentage < 0 || targetingSettings.Audience.DefaultRolloutPercentage > 100)
            {
                paramName = $"{targetingSettings.Audience}.{targetingSettings.Audience.DefaultRolloutPercentage}";

                reason = OutOfRange;

                return false;
            }

            if (targetingSettings.Audience.Groups != null)
            {
                int index = 0;

                foreach (GroupRollout groupRollout in targetingSettings.Audience.Groups)
                {
                    index++;

                    if (groupRollout.RolloutPercentage < 0 || groupRollout.RolloutPercentage > 100)
                    {
                        //
                        // Audience.Groups[1].RolloutPercentage
                        paramName = $"{targetingSettings.Audience}.{targetingSettings.Audience.Groups}[{index}].{groupRollout.RolloutPercentage}";

                        reason = OutOfRange;

                        return false;
                    }
                }
            }

            return true;
        }


        /// <summary>
        /// Determines if a given context id should be targeted based off the provided percentage
        /// </summary>
        /// <param name="contextId">A context identifier that determines what the percentage is applicable for</param>
        /// <param name="percentage">The total percentage of possible context identifiers that should be targeted</param>
        /// <returns>A boolean representing if the context identifier should be targeted</returns>
        private static bool IsTargeted(string contextId, double percentage)
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
