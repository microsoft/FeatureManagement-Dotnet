using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.FeatureManagement.Telemetry
{
    internal static class FeatureEvaluationTelemetry
    {
        private static readonly string EvaluationEventVersion = "1.0.0";

        /// <summary>
        /// Handles an evaluation event by adding it as an activity event to the current Activity.
        /// </summary>
        /// <param name="evaluationEvent">The <see cref="EvaluationEvent"/> to publish as an <see cref="ActivityEvent"/></param>
        /// <param name="logger">Optional logger to log warnings to</param>
        public static void Publish(EvaluationEvent evaluationEvent, ILogger logger)
        {
            if (Activity.Current == null)
            {
                throw new InvalidOperationException("An Activity must be created before calling this method.");
            }

            if (evaluationEvent == null)
            {
                throw new ArgumentNullException(nameof(evaluationEvent));
            }

            if (evaluationEvent.FeatureDefinition == null)
            {
                throw new ArgumentNullException(nameof(evaluationEvent.FeatureDefinition));
            }

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

            // VariantAssignmentPercentage
            if (evaluationEvent.VariantAssignmentReason == VariantAssignmentReason.DefaultWhenEnabled)
            {
                // If the variant was assigned due to DefaultWhenEnabled, the percentage reflects the unallocated percentiles
double allocatedPercentage = evaluationEvent.FeatureDefinition.Allocation?.Percentile?.Sum(p => p.To - p.From) ?? 0;    

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

            var activityEvent = new ActivityEvent("FeatureFlag", DateTimeOffset.UtcNow, tags);

            Activity.Current.AddEvent(activityEvent);
        }
    }
}
