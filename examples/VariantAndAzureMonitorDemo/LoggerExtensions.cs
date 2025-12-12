using Microsoft.Extensions.Logging;

#nullable enable

namespace VariantAndAzureMonitorDemo
{
    public static class LoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception?> _vote = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, "microsoft.custom_event.name"),
            "{microsoft.custom_event.name}");

        private static readonly Action<ILogger, string, string, long, Exception?> _checkout = LoggerMessage.Define<string, string, long>(
            LogLevel.Information,
            new EventId(1, "microsoft.custom_event.name"),
            "{microsoft.custom_event.name} {success} {amount}");
        public static void LogVote(this ILogger logger)
        {
            _vote(logger, "Vote", null);
        }

        public static void LogCheckout(this ILogger logger, long amount)
        {
            _checkout(logger, "checkout", "yes", amount, null);
        }
    }
}
