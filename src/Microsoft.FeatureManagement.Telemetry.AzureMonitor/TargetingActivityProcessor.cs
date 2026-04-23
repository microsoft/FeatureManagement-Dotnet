// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using OpenTelemetry;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.FeatureManagement.Telemetry.AzureMonitor
{
    /// <summary>
    /// An OpenTelemetry processor that adds the targeting id to the current activity.
    /// </summary>
    public class TargetingActivityProcessor : BaseProcessor<Activity>
    {
        private const string TargetingIdKey = "TargetingId";

        /// <summary>
        /// Called when an activity ends.
        /// </summary>
        /// <param name="activity">The activity that ended.</param>
        public override void OnEnd(Activity activity)
        {
            if (activity == null)
            {
                return;
            }

            string targetingId = activity.Baggage.FirstOrDefault(t => t.Key == TargetingIdKey).Value;

            if (!string.IsNullOrEmpty(targetingId))
            {
                activity.SetTag(TargetingIdKey, targetingId);
            }
        }
    }
}
