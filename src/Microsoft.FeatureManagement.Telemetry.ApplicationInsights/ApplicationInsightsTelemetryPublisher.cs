// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.Telemetry.ApplicationInsights
{
    /// <summary>
    /// Used to publish data from evaluation events to Application Insights
    /// </summary>
    public class ApplicationInsightsTelemetryPublisher : ITelemetryPublisher
    {
        private readonly string _eventName = "FeatureEvaluation";
        private readonly TelemetryClient _telemetryClient;

        public ApplicationInsightsTelemetryPublisher(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        }

        /// <summary>
        /// Publishes a custom event to Application Insights using data from the given evaluation event.
        /// </summary>
        /// <param name="evaluationEvent"> The event to publish.</param>
        /// <param name="cancellationToken"> A cancellation token.</param>
        /// <returns>Returns a ValueTask that represents the asynchronous operation</returns>
        public ValueTask PublishEvent(EvaluationEvent evaluationEvent, CancellationToken cancellationToken)
        {
            ValidateEvent(evaluationEvent);

            FeatureDefinition featureDefinition = evaluationEvent.FeatureDefinition;

            Dictionary<string, string> properties = new Dictionary<string, string>()
            {
                { "FeatureName", featureDefinition.Name },
                { "IsEnabled", evaluationEvent.IsEnabled.ToString() },
                { "Variant", evaluationEvent.Variant?.Name }
            };

            if (featureDefinition.ETag != null)
            {
                properties.Add("ETag", featureDefinition.ETag);
            }

            if (featureDefinition.Label != null)
            {
                properties.Add("Label", featureDefinition.Label);
            }

            if (featureDefinition.Tags != null)
            {
                foreach (KeyValuePair<string, string> kvp in featureDefinition.Tags)
                {
                    properties["Tags." + kvp.Key] = kvp.Value;
                }
            }

            _telemetryClient.TrackEvent(_eventName, properties);


            return new ValueTask();
        }

        private void ValidateEvent(EvaluationEvent evaluationEvent)
        {
            if (evaluationEvent == null)
            {
                throw new ArgumentNullException(nameof(evaluationEvent));
            }

            if (evaluationEvent.FeatureDefinition == null)
            {
                throw new ArgumentNullException(nameof(evaluationEvent.FeatureDefinition));
            }
        }
    }
}