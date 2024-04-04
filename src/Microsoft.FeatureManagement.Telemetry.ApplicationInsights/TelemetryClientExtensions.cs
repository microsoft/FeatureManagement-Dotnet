// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.FeatureManagement.FeatureFilters;

namespace Microsoft.ApplicationInsights
{
    /// <summary>
    /// Provides extension methods for tracking events with <see cref="TargetingContext"/>.
    /// </summary>
    public static class TelemetryClientExtensions
    {
        /// <summary>
        /// Extension method to track an event with <see cref="TargetingContext"/>.
        /// </summary>
        public static void TrackEvent(this TelemetryClient telemetryClient, string eventName, TargetingContext targetingContext, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            ValidateTargetingContext(targetingContext);

            if (properties == null)
            {
                properties = new Dictionary<string, string>();
            }

            properties["TargetingId"] = targetingContext.UserId;

            telemetryClient.TrackEvent(eventName, properties, metrics);
        }

        /// <summary>
        /// Extension method to track an <see cref="EventTelemetry"/> with <see cref="TargetingContext"/>.
        /// </summary>
        public static void TrackEvent(this TelemetryClient telemetryClient, EventTelemetry telemetry, TargetingContext targetingContext)
        {
            ValidateTargetingContext(targetingContext);

            if (telemetry == null)
            {
                telemetry = new EventTelemetry();
            }

            telemetry.Properties["TargetingId"] = targetingContext.UserId;

            telemetryClient.TrackEvent(telemetry);
        }

        private static void ValidateTargetingContext(TargetingContext targetingContext)
        {
            if (targetingContext == null)
            {
                throw new ArgumentNullException(nameof(targetingContext));
            }
        }
    }
}
