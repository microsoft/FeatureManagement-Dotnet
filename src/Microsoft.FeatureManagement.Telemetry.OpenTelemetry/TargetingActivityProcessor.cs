// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using OpenTelemetry;
using System.Diagnostics;

namespace Microsoft.FeatureManagement.Telemetry.OpenTelemetry
{
    /// <summary>
    /// Adds targeting information to outgoing OpenTelemetry <see cref="Activity"/> spans.
    /// </summary>
    public class TargetingActivityProcessor : BaseProcessor<Activity>
    {
        private const string TargetingIdKey = "TargetingId";

        /// <summary>
        /// When an <see cref="Activity"/> ends, adds targeting information to it if available.
        /// </summary>
        /// <param name="activity">The <see cref="Activity"/> being processed.</param>
        public override void OnEnd(Activity activity)
        {
            if (activity == null)
            {
                return;
            }

            // Extract the targeting id from the activity's baggage
            string targetingId = activity.Baggage.FirstOrDefault(t => t.Key == TargetingIdKey).Value;

            // Don't modify the activity if there's no available targeting id
            if (string.IsNullOrEmpty(targetingId))
            {
                return;
            }

            activity.SetTag(TargetingIdKey, targetingId);
        }
    }
}
