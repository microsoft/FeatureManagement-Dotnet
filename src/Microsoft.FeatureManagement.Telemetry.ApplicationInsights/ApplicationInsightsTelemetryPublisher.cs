// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.ApplicationInsights;

namespace Microsoft.FeatureManagement.Telemetry.ApplicationInsights
{
    /// <summary>
    /// Used to publish data from evaluation events to Application Insights
    /// </summary>
    public class ApplicationInsightsTelemetryPublisher : ITelemetryPublisher
    {
        private const string _eventName = "FeatureEvaluation";
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

            var properties = new Dictionary<string, string>()
            {
                { "FeatureName", featureDefinition.Name },
                { "IsEnabled", evaluationEvent.IsEnabled.ToString() }
            };

            if (evaluationEvent.Variant != null)
            {
                properties["Variant"] = evaluationEvent.Variant.Name;
            }

            if (evaluationEvent.AssignmentReason != AssignmentReason.None)
            {
                properties["AssignmentReason"] = ToString(evaluationEvent.AssignmentReason);
            }

            if (featureDefinition.TelemetryMetadata != null)
            {
                foreach (KeyValuePair<string, string> kvp in featureDefinition.TelemetryMetadata)
                {
                    properties[kvp.Key] = kvp.Value;
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

        private static string ToString(AssignmentReason reason)
        {
            const string None = "None";
            const string DisabledDefault = "DisabledDefault";
            const string EnabledDefault = "EnabledDefault";
            const string User = "User";
            const string Group = "Group";
            const string Percentile = "Percentile";

            return reason switch
            {
                AssignmentReason.None => None,
                AssignmentReason.DisabledDefault => DisabledDefault,
                AssignmentReason.EnabledDefault => EnabledDefault,
                AssignmentReason.User => User,
                AssignmentReason.Group => Group,
                AssignmentReason.Percentile => Percentile,
                _ => throw new ArgumentException("Invalid assignment reason.", nameof(reason))
            };
        }
    }
}