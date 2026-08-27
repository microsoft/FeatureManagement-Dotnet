// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using OpenTelemetry;
using OpenTelemetry.Logs;
using System.Diagnostics;

namespace Microsoft.FeatureManagement.Telemetry.OpenTelemetry
{
    /// <summary>
    /// Adds targeting information to outgoing OpenTelemetry <see cref="LogRecord"/>s.
    /// </summary>
    public class TargetingLogProcessor : BaseProcessor<LogRecord>
    {
        private const string TargetingIdKey = "TargetingId";

        /// <summary>
        /// When a <see cref="LogRecord"/> ends, adds targeting information to it if available.
        /// </summary>
        /// <param name="data">The <see cref="LogRecord"/> being processed.</param>
        public override void OnEnd(LogRecord data)
        {
            if (data == null)
            {
                return;
            }

            Activity activity = Activity.Current;

            if (activity == null)
            {
                return;
            }

            // Extract the targeting id from the current activity's baggage
            string targetingId = activity.Baggage.FirstOrDefault(t => t.Key == TargetingIdKey).Value;

            // Don't modify the log record if there's no available targeting id
            if (string.IsNullOrEmpty(targetingId))
            {
                return;
            }

            // Don't overwrite a TargetingId attribute the log record already carries
            if (data.Attributes != null && data.Attributes.Any(attribute => attribute.Key == TargetingIdKey))
            {
                return;
            }

            var attributes = new List<KeyValuePair<string, object>>(data.Attributes ?? Array.Empty<KeyValuePair<string, object>>())
            {
                new KeyValuePair<string, object>(TargetingIdKey, targetingId)
            };

            data.Attributes = attributes;
        }
    }
}
