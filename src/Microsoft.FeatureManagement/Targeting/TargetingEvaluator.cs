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

        const string OutOfRange = "The value is out of the accepted range.";
        const string RequiredParameter = "Value cannot be null.";

        /// <summary>
        /// Checks if a provided targeting context should be targeted given targeting settings.
        /// </summary>
        public static bool IsTargeted(ITargetingContext targetingContext, TargetingFilterSettings settings, bool ignoreCase, string hint)
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

            if (settings.Audience.Exclusion != null)
            {
                //
                // Check if the user is in the exclusion directly
                if (targetingContext.UserId != null &&
                    settings.Audience.Exclusion.Users != null &&
                    settings.Audience.Exclusion.Users.Any(user => targetingContext.UserId.Equals(user, GetComparisonType(ignoreCase))))
                {
                    return false;
                }

                //
                // Check if the user is in a group within exclusion
                if (targetingContext.Groups != null &&
                    settings.Audience.Exclusion.Groups != null &&
                    settings.Audience.Exclusion.Groups.Any(group => targetingContext.Groups.Any(g => g?.Equals(group, GetComparisonType(ignoreCase)) ?? false)))
                {
                    return false;
                }
            }

            //
            // Check if the user is being targeted directly
            if (settings.Audience.Users != null &&
                IsTargeted(
                    targetingContext.UserId,
                    settings.Audience.Users,
                    ignoreCase))
            {
                return true;
            }

            //
            // Check if the user is in a group that is being targeted
            if (settings.Audience.Groups != null &&
                IsTargeted(
                    targetingContext,
                    settings.Audience.Groups,
                    ignoreCase,
                    hint))
            {
                return true;
            }

            //
            // Check if the user is being targeted by a default rollout percentage
            return IsTargeted(
                targetingContext,
                settings.Audience.DefaultRolloutPercentage,
                ignoreCase,
                hint);
        }

        /// <summary>
        /// Determines if a targeting context is targeted by presence in a list of users
        /// </summary>
        public static bool IsTargeted(
            string userId,
            IEnumerable<string> users,
            bool ignoreCase)
        {
            if (users == null)
            {
                throw new ArgumentNullException(nameof(users));
            }

            if (userId != null &&
                users.Any(user => userId.Equals(user, GetComparisonType(ignoreCase))))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if targeting context is targeted by presence in a list of groups
        /// </summary>
        public static bool IsTargeted(
            IEnumerable<string> sourceGroups,
            IEnumerable<string> targetedGroups,
            bool ignoreCase)
        {
            if (targetedGroups == null)
            {
                throw new ArgumentNullException(nameof(targetedGroups));
            }

            if (sourceGroups != null)
            {
                IEnumerable<string> normalizedGroups = ignoreCase ?
                    sourceGroups.Select(g => g.ToLower()) :
                    sourceGroups;

                foreach (string group in normalizedGroups)
                {
                    string allocationGroup = targetedGroups.FirstOrDefault(g => g.Equals(group, GetComparisonType(ignoreCase)));

                    if (allocationGroup != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determine if a targeting context is targeted by presence in a group and its rollout percentage
        /// </summary>
        public static bool IsTargeted(
            ITargetingContext targetingContext,
            IEnumerable<GroupRollout> groups,
            bool ignoreCase,
            string hint)
        {
            if (targetingContext == null)
            {
                throw new ArgumentNullException(nameof(targetingContext));
            }

            if (groups == null)
            {
                throw new ArgumentNullException(nameof(groups));
            }

            if (string.IsNullOrEmpty(hint))
            {
                throw new ArgumentNullException(nameof(hint));
            }

            string userId = ignoreCase ?
                targetingContext.UserId.ToLower() :
                targetingContext.UserId;

            if (targetingContext.Groups != null)
            {
                IEnumerable<string> normalizedGroups = ignoreCase ?
                    targetingContext.Groups.Select(g => g.ToLower()) :
                    targetingContext.Groups;

                foreach (string group in normalizedGroups)
                {
                    GroupRollout groupRollout = groups.FirstOrDefault(g => g.Name.Equals(group, GetComparisonType(ignoreCase)));

                    if (groupRollout != null)
                    {
                        string audienceContextId = $"{userId}\n{hint}\n{group}";

                        if (IsTargeted(audienceContextId, 0, groupRollout.RolloutPercentage))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if a targeting context is targeted by presence in a default rollout percentage.
        /// </summary>
        public static bool IsTargeted(
            ITargetingContext targetingContext,
            double defaultRolloutPercentage,
            bool ignoreCase,
            string hint)
        {
            if (targetingContext == null)
            {
                throw new ArgumentNullException(nameof(targetingContext));
            }

            if (string.IsNullOrEmpty(hint))
            {
                throw new ArgumentNullException(nameof(hint));
            }

            string userId = ignoreCase ?
                targetingContext.UserId.ToLower() :
                targetingContext.UserId;

            string defaultContextId = $"{userId}\n{hint}";

            return IsTargeted(defaultContextId, 0, defaultRolloutPercentage);
        }

        /// <summary>
        /// Performs validation of targeting settings.
        /// </summary>
        /// <param name="targetingSettings">The settings to validate.</param>
        /// <param name="paramName">The name of the invalid setting, if any.</param>
        /// <param name="reason">The reason that the setting is invalid.</param>
        /// <returns>True if the provided settings are valid. False if the provided settings are invalid.</returns>
        private static bool TryValidateSettings(TargetingFilterSettings targetingSettings, out string paramName, out string reason)
        {
            paramName = null;

            reason = null;

            if (targetingSettings == null)
            {
                paramName = nameof(targetingSettings);

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
        /// Determines if a given context id should be targeted based off the provided percentage range
        /// </summary>
        public static bool IsTargeted(ITargetingContext targetingContext, double from, double to, bool ignoreCase, string hint)
        {
            if (targetingContext == null)
            {
                throw new ArgumentNullException(nameof(targetingContext));
            }

            if (string.IsNullOrEmpty(hint))
            {
                throw new ArgumentNullException(nameof(hint));
            }

            if (from < 0 || from > 100)
            {
                throw new ArgumentException(OutOfRange, nameof(from));
            }

            if (to < 0 || to > 100)
            {
                throw new ArgumentException(OutOfRange, nameof(to));
            }

            if (from > to)
            {
                throw new ArgumentException($"Value of {nameof(from)} cannot be larger than value of {nameof(to)}.");
            }

            string userId = ignoreCase ?
                targetingContext.UserId.ToLower() :
                targetingContext.UserId;

            string contextId = $"{userId}\n{hint}";

            return IsTargeted(contextId, from, to);
        }

        /// <summary>
        /// Determines if a given context id should be targeted based off the provided percentage
        /// </summary>
        /// <param name="contextId">A context identifier that determines what the percentage is applicable for</param>
        /// <param name="from">The lower bound of the percentage for which the context identifier will be targeted</param>
        /// <param name="to">The upper bound of the percentage for which the context identifier will be targeted</param>
        /// <returns>A boolean representing if the context identifier should be targeted</returns>
        private static bool IsTargeted(string contextId, double from, double to)
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

            //
            // Handle edge case of exact 100 bucket
            if (to == 100)
            {
                return contextPercentage >= from;
            }

            return contextPercentage >= from && contextPercentage < to;
        }
    }
}
