// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using OpenTelemetry;
using OpenTelemetry.Logs;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.FeatureManagement.Telemetry.AzureMonitor
{
    /// <summary>
    /// An OpenTelemetry processor that adds the targeting id to the current log record.
    /// </summary>
    public class TargetingLogProcessor : BaseProcessor<LogRecord>
    {
        private const string TargetingIdKey = "TargetingId";

        /// <summary>
        /// Called when a log record is ended.
        /// </summary>
        /// <param name="logRecord">The log record that ended.</param>
        public override void OnEnd(LogRecord logRecord)
        {
            if (logRecord == null)
            {
                return;
            }

            string targetingId = Activity.Current?.Baggage.FirstOrDefault(t => t.Key == TargetingIdKey).Value;

            if (!string.IsNullOrEmpty(targetingId))
            {
                var attributes = new List<KeyValuePair<string, object>>(logRecord.Attributes ?? Enumerable.Empty<KeyValuePair<string, object>>());

                attributes.Add(new KeyValuePair<string, object>(TargetingIdKey, targetingId));

                logRecord.Attributes = attributes;
            }
        }
    }
}
