// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.Telemetry
{
    /// <summary>
    /// A publisher of telemetry events.
    /// </summary>
    public interface ITelemetryPublisher
    {
        /// <summary>
        /// Handles an EvaluationEvent and publishes it to the configured telemetry channel.
        /// </summary>
        /// <param name="evaluationEvent"> The event to publish.</param>
        /// <param name="cancellationToken"> A cancellation token.</param>
        /// <returns>ValueTask</returns>
        public ValueTask PublishEvent(EvaluationEvent evaluationEvent, CancellationToken cancellationToken);
    }
}
