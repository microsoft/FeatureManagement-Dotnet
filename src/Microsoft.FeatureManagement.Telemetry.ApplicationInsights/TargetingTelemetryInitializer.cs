// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Diagnostics;

namespace Microsoft.FeatureManagement.Telemetry.ApplicationInsights
{
    /// <summary>
    /// Used to add targeting information to outgoing Application Insights telemetry.
    /// </summary>
    public class TargetingTelemetryInitializer : ITelemetryInitializer
    {
        private const string TargetingIdKey = $"Microsoft.FeatureManagement.TargetingId";

        /// <summary>
        /// When telemetry is initialized, adds targeting information to all relevant telemetry.
        /// </summary>
        /// <param name="telemetry">The <see cref="ITelemetry"/> to be initialized.</param>
        /// <exception cref="ArgumentNullException">Thrown if the any param is null.</exception>
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            // Extract the targeting id from the current activity's baggage
            string targetingId = Activity.Current?.Baggage.FirstOrDefault(t => t.Key == TargetingIdKey).Value;

            // Don't modify telemetry if there's no available targeting id
            if (string.IsNullOrEmpty(targetingId))
            {
                return;
            }

            // Telemetry.Properties is deprecated in favor of ISupportProperties
            if (telemetry is ISupportProperties telemetryWithSupportProperties)
            {
                telemetryWithSupportProperties.Properties["TargetingId"] = targetingId;
            }
        }
    }
}
