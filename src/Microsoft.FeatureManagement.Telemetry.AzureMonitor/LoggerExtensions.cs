// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Logging;

namespace Microsoft.FeatureManagement.Telemetry.AzureMonitor
{
    /// <summary>
    /// Logger extensions for feature flag evaluation events in Azure Monitor.
    /// </summary>
    internal static class LoggerExtensions
    {
        /// <summary>
        /// Logs a feature flag evaluation event to Azure Monitor.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="eventName">The name of the event (e.g., "FeatureEvaluation").</param>
        /// <param name="properties">The dictionary of properties to log.</param>
        public static void LogFeatureEvaluation(
            this ILogger logger,
            string eventName,
            Dictionary<string, object> properties)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentException("Event name cannot be null or empty.", nameof(eventName));
            }

            // Build the message template dynamically with placeholders for each property
            var templateBuilder = new System.Text.StringBuilder("{microsoft.custom_event.name}");
            var args = new List<object> { eventName };

            if (properties != null && properties.Count > 0)
            {
                foreach (var kvp in properties)
                {
                    templateBuilder.Append($" {{{kvp.Key}}}");
                    args.Add(kvp.Value);
                }
            }

            // Use structured logging to ensure each property becomes a separate custom dimension
            logger.Log(
                LogLevel.Information,
                new EventId(1, "microsoft.custom_event.name"),
                templateBuilder.ToString(),
                args.ToArray());
        }
    }
}
