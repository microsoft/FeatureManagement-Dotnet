// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.FeatureManagement.FeatureFilters;

namespace Microsoft.FeatureManagement.Telemetry.ApplicationInsights
{
    /// <summary>
    /// Provides extension methods for tracking events with TargetingContext.
    /// </summary>
    public static class ApplicationInsightsTelemetryExtensions
    {
        /// <summary>
        /// Extension method to track an event with <see cref="TargetingContext"/>.
        /// </summary>
        public static void TrackEvent(this TelemetryClient telemetryClient, string eventName, TargetingContext targetingContext, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
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
            if (telemetry == null)
            {
                telemetry = new EventTelemetry();
            }

            telemetry.Properties["TargetingId"] = targetingContext.UserId;

            telemetryClient.TrackEvent(telemetry);
        }

        /// <summary>
        /// Extension method to track a metric with <see cref="TargetingContext"/>.
        /// </summary>
        public static void TrackMetric(this TelemetryClient telemetryClient, string name, double value, TargetingContext targetingContext, IDictionary<string, string> properties = null)
        {
            if (properties == null)
            {
                properties = new Dictionary<string, string>();
            }

            properties["TargetingId"] = targetingContext.UserId;

            telemetryClient.TrackMetric(name, value, properties);
        }

        /// <summary>
        /// Extension method to track a <see cref="MetricTelemetry"/> with <see cref="TargetingContext"/>.
        /// </summary>
        public static void TrackMetric(this TelemetryClient telemetryClient, MetricTelemetry telemetry, TargetingContext targetingContext)
        {
            if (telemetry == null)
            {
                telemetry = new MetricTelemetry();
            }

            telemetry.Properties["TargetingId"] = targetingContext.UserId;

            telemetryClient.TrackMetric(telemetry);
        }
    }
}
