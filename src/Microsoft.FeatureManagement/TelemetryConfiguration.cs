using Microsoft.FeatureManagement.Telemetry;
using System.Collections.Generic;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Defines telemetry related configuration settings available for features.
    /// </summary>
    public class TelemetryConfiguration
    {
        /// <summary>
        /// A flag to enable or disable sending <see cref="EvaluationEvent"/> events to registered <see cref="ITelemetryPublisher"/>s.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// A container for metadata relevant to telemetry.
        /// </summary>
        public IReadOnlyDictionary<string, string> Metadata { get; set; }
    }
}
