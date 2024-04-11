// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.ApplicationInsights;
using Microsoft.FeatureManagement.Telemetry.ApplicationInsights;
using System.Diagnostics;

namespace Microsoft.FeatureManagement.Telemetry
{
    /// <summary>
    /// Used to publish data from evaluation events to Application Insights
    /// </summary>
    public class ApplicationInsightsTelemetryPublisher : ITelemetryPublisher
    {
        private const string _eventName = "FeatureEvaluation";
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Creates an instance of the Application Insights telemetry publisher
        /// </summary>
        /// <param name="telemetryClient">The underlying telemetry client that will be used to send data to Application Insights</param>
        /// <exception cref="ArgumentNullException">Thrown if the provided telemetry client is null.</exception>
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
                { "Enabled", evaluationEvent.Enabled.ToString() }
            };

            if (evaluationEvent.TargetingContext != null)
            {
                properties[Constants.TargetingIdKey] = evaluationEvent.TargetingContext.UserId;
            }

            if (evaluationEvent.VariantAssignmentReason != VariantAssignmentReason.None)
            {
                properties["Variant"] = evaluationEvent.Variant?.Name;

                properties["VariantAssignmentReason"] = ToString(evaluationEvent.VariantAssignmentReason);
            }

            if (featureDefinition.Telemetry.Metadata != null)
            {
                foreach (KeyValuePair<string, string> kvp in featureDefinition.Telemetry.Metadata)
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
                throw new ArgumentException(
                    "Feature definition is required.",
                    nameof(evaluationEvent));
            }

            if (evaluationEvent.FeatureDefinition.Telemetry == null)
            {
                throw new ArgumentException(
                    "Feature definition telemetry configuration is required.",
                    nameof(evaluationEvent));
            }
        }

        private static string ToString(VariantAssignmentReason reason)
        {
            Debug.Assert(reason != VariantAssignmentReason.None);

            const string DefaultWhenDisabled = "DefaultWhenDisabled";
            const string DefaultWhenEnabled = "DefaultWhenEnabled";
            const string User = "User";
            const string Group = "Group";
            const string Percentile = "Percentile";

            return reason switch
            {
                VariantAssignmentReason.DefaultWhenDisabled => DefaultWhenDisabled,
                VariantAssignmentReason.DefaultWhenEnabled => DefaultWhenEnabled,
                VariantAssignmentReason.User => User,
                VariantAssignmentReason.Group => Group,
                VariantAssignmentReason.Percentile => Percentile,
                _ => throw new ArgumentException("Invalid assignment reason.", nameof(reason))
            };
        }
    }
}