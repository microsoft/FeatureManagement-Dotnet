// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Logging;

namespace OpenTelemetryDemo
{
    /// <summary>
    /// High-performance, source-generator-style logging (<see cref="LoggerMessage.Define"/>) for
    /// this app's custom events. Each event's message template includes the
    /// "microsoft.custom_event.name" placeholder, which becomes a `LogRecord` attribute that the
    /// Azure Monitor OpenTelemetry Exporter (if configured) uses to classify the log as a custom
    /// event in Application Insights, rather than a plain trace.
    /// </summary>
    public static class LoggerExtensions
    {
        private const string AzureMonitorCustomEventNameKey = "microsoft.custom_event.name";

        private static readonly Action<ILogger, string, int, Exception> _vote = LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(1, AzureMonitorCustomEventNameKey),
            "{" + AzureMonitorCustomEventNameKey + "} {ImageRating}");

        private static readonly Action<ILogger, string, string, long, Exception> _checkout = LoggerMessage.Define<string, string, long>(
            LogLevel.Information,
            new EventId(2, AzureMonitorCustomEventNameKey),
            "{" + AzureMonitorCustomEventNameKey + "} {success} {checkoutAmount}");

        public static void LogVote(this ILogger logger, int rating)
        {
            _vote(logger, "Vote", rating, null);
        }

        public static void LogCheckout(this ILogger logger, long amount)
        {
            _checkout(logger, "checkout", "yes", amount, null);
        }
    }
}
