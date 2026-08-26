// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests.FeatureManagement.Telemetry.OpenTelemetry
{
    /// <summary>
    /// A single captured invocation of <see cref="ILogger.Log{TState}(LogLevel, EventId, TState, System.Exception, System.Func{TState, System.Exception, string})"/>.
    /// </summary>
    public class CapturedLogRecord
    {
        public LogLevel LogLevel { get; set; }

        public EventId EventId { get; set; }

        public IReadOnlyList<KeyValuePair<string, object>> State { get; set; }
    }

    /// <summary>
    /// An <see cref="ILoggerProvider"/> that captures every log record it receives, for test assertions.
    /// </summary>
    public class CapturingLoggerProvider : ILoggerProvider
    {
        public List<CapturedLogRecord> Records { get; } = new List<CapturedLogRecord>();

        public ILogger CreateLogger(string categoryName)
        {
            return new CapturingLogger(this);
        }

        public void Dispose()
        {
        }

        private class CapturingLogger : ILogger
        {
            private readonly CapturingLoggerProvider _provider;

            public CapturingLogger(CapturingLoggerProvider provider)
            {
                _provider = provider;
            }

            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception exception, System.Func<TState, System.Exception, string> formatter)
            {
                IReadOnlyList<KeyValuePair<string, object>> capturedState =
                    (state as IEnumerable<KeyValuePair<string, object>>)?.ToList();

                _provider.Records.Add(new CapturedLogRecord
                {
                    LogLevel = logLevel,
                    EventId = eventId,
                    State = capturedState
                });
            }
        }
    }
}
