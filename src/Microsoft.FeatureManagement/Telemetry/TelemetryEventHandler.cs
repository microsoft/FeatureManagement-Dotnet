using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Microsoft.FeatureManagement.Telemetry
{
    internal static class TelemetryEventHandler
    {
        private static readonly string EvaluationEventVersion = "1.0.0";

        /// <summary>
        /// Handles an evaluation event by adding it as an activity event to the current Activity.
        /// </summary>
        /// <param name="evaluationEvent">The <see cref="EvaluationEvent"/> to publish as an <see cref="ActivityEvent"/></param>
        /// <param name="logger">Optional logger to log warnings to</param>
        public static void HandleEvaluationEvent(EvaluationEvent evaluationEvent, ILogger logger)
        {
            Debug.Assert(evaluationEvent != null);
            Debug.Assert(evaluationEvent.FeatureDefinition != null);

            var tags = new ActivityTagsCollection()
            {
                { "FeatureName", evaluationEvent.FeatureDefinition.Name },
                { "Enabled", evaluationEvent.Enabled },
                { "VariantAssignmentReason", evaluationEvent.VariantAssignmentReason },
                { "Version", EvaluationEventVersion }
            };

            if (!string.IsNullOrEmpty(evaluationEvent.TargetingContext?.UserId))
            {
                tags["TargetingId"] = evaluationEvent.TargetingContext.UserId;
            }

            if (!string.IsNullOrEmpty(evaluationEvent.Variant?.Name))
            {
                tags["Variant"] = evaluationEvent.Variant.Name;
            }

            if (evaluationEvent.FeatureDefinition.Telemetry.Metadata != null)
            {
                foreach (KeyValuePair<string, string> kvp in evaluationEvent.FeatureDefinition.Telemetry.Metadata)
                {
                    if (tags.ContainsKey(kvp.Key))
                    {
                        logger?.LogWarning($"{kvp.Key} from telemetry metadata will be ignored, as it would override an existing key.");

                        continue;
                    }

                    tags[kvp.Key] = kvp.Value;
                }
            }

            // VariantAllocationPercentage
            if (evaluationEvent.VariantAssignmentReason == VariantAssignmentReason.DefaultWhenEnabled)
            {
                // If the variant was assigned due to DefaultWhenEnabled, the percentage is 100% - all allocated percentiles
                double allocatedPercentage = 0;

                if (evaluationEvent.FeatureDefinition.Allocation?.Percentile != null)
                {
                    allocatedPercentage += evaluationEvent.FeatureDefinition.Allocation.Percentile
                        .Sum(p => p.To - p.From);
                }

                tags["VariantAssignmentPercentage"] = 100 - allocatedPercentage;
            }
            else if (evaluationEvent.VariantAssignmentReason == VariantAssignmentReason.Percentile)
            {
                // If the variant was assigned due to Percentile, the percentage is the sum of the allocated percentiles for the given variant
                if (evaluationEvent.FeatureDefinition.Allocation?.Percentile != null)
                {
                    tags["VariantAssignmentPercentage"] = evaluationEvent.FeatureDefinition.Allocation.Percentile
                        .Where(p => p.Variant == evaluationEvent.Variant?.Name)
                        .Sum(p => p.To - p.From);
                }
            }

            // DefaultWhenEnabled
            if (evaluationEvent.FeatureDefinition.Allocation?.DefaultWhenEnabled != null)
            {
                tags["DefaultWhenEnabled"] = evaluationEvent.FeatureDefinition.Allocation.DefaultWhenEnabled;
            }

            // AllocationId
            string allocationId = GenerateAllocationId(evaluationEvent.FeatureDefinition);

            if (allocationId != null)
            {
                tags["AllocationId"] = allocationId;
            }

            var activityEvent = new ActivityEvent("FeatureFlag", DateTimeOffset.UtcNow, tags);

            Activity.Current.AddEvent(activityEvent);
        }

        private static string GenerateAllocationId(FeatureDefinition featureDefinition)
        {
            StringBuilder inputBuilder = new StringBuilder();

            // Seed
            inputBuilder.Append($"seed={featureDefinition.Allocation?.Seed ?? ""}");

            var allocatedVariants = new HashSet<string>();

            // DefaultWhenEnabled
            if (featureDefinition.Allocation?.DefaultWhenEnabled != null)
            {
                allocatedVariants.Add(featureDefinition.Allocation.DefaultWhenEnabled);
            }

            inputBuilder.Append("\n");
            inputBuilder.Append($"default_when_enabled={featureDefinition.Allocation?.DefaultWhenEnabled ?? ""}");

            // Percentiles
            inputBuilder.Append("\n");
            inputBuilder.Append("percentiles=");

            if (featureDefinition.Allocation?.Percentile != null && featureDefinition.Allocation.Percentile.Any())
            {
                var sortedPercentiles = featureDefinition.Allocation.Percentile
                    .Where(p => p.From != p.To)
                    .OrderBy(p => p.From)
                    .ToList();

                allocatedVariants.UnionWith(sortedPercentiles.Select(p => p.Variant));

                inputBuilder.Append(string.Join(";", sortedPercentiles.Select(p => $"{p.From},{p.Variant},{p.To}")));
            }

            // Variants
            inputBuilder.Append("\n");
            inputBuilder.Append("variants=");

            if (allocatedVariants.Any() && featureDefinition.Variants != null && featureDefinition.Variants.Any())
            {
                var sortedVariants = featureDefinition.Variants
                    .Where(variant => allocatedVariants.Contains(variant.Name))
                    .OrderBy(variant => variant.Name)
                    .ToList();

                inputBuilder.Append(string.Join(";", sortedVariants.Select(v => $"{v.Name},{v.ConfigurationValue?.Value}")));
            }

            // If there's not a special seed and no variants allocated, return null
            if (featureDefinition.Allocation?.Seed == null &&
                !allocatedVariants.Any())
            {
                return null;
            }

            // Example input string
            // input == "seed=123abc\ndefault_when_enabled=Control\npercentiles=0,Control,20;20,Test,100\nvariants=Control,standard;Test,special"
            string input = inputBuilder.ToString();

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                byte[] truncatedHash = new byte[15];
                Array.Copy(hash, truncatedHash, 15);
                return truncatedHash.ToBase64Url();
            }
        }
    }
}
