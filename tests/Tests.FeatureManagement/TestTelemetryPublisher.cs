// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.Telemetry;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.Tests
{
    public class TestTelemetryPublisher : ITelemetryPublisher
    {
        public EvaluationEvent evaluationEventCache { get; private set; }

        public ValueTask PublishEvent(EvaluationEvent evaluationEvent, CancellationToken cancellationToken)
        {
            evaluationEventCache = evaluationEvent;

            return new ValueTask();
        }
    }
}