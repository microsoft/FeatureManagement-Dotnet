// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.Telemetry;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.Tests
{
    public class TestTelemetryPublisher : ITelemetryPublisher
    {
        private readonly string _eventName = "FeatureEvaluation";
        
        public EvaluationEvent evalationEventCache { get; private set; }

        public ValueTask PublishEvent(EvaluationEvent evaluationEvent, CancellationToken cancellationToken)
        {
            evalationEventCache = evaluationEvent;

            return new ValueTask();
        }
    }
}