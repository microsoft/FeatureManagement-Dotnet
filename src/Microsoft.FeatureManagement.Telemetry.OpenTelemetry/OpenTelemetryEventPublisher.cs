// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Microsoft.FeatureManagement.Telemetry.OpenTelemetry
{
    /// <summary>
    /// Listens to <see cref="Activity"/> events from feature management and emits them as OpenTelemetry log-based custom events.
    /// </summary>
    internal sealed class OpenTelemetryEventPublisher : IDisposable
    {
        private const string AzureMonitorCustomEventNameKey = "microsoft.custom_event.name";
        private const string FeatureEvaluationEventName = "FeatureEvaluation";
        private const string FeatureManagementActivitySourceName = "Microsoft.FeatureManagement";
        private const string FeatureFlagActivityEventName = "FeatureFlag";

        private readonly ILogger _logger;
        private readonly ActivityListener _activityListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenTelemetryEventPublisher"/> class.
        /// </summary>
        /// <param name="logger">The logger used to emit the OpenTelemetry log-based custom event.</param>
        public OpenTelemetryEventPublisher(ILogger<OpenTelemetryEventPublisher> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _activityListener = new ActivityListener
            {
                ShouldListenTo = (activitySource) => activitySource.Name == FeatureManagementActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
                ActivityStopped = (activity) =>
                {
                    ActivityEvent? evaluationEvent = activity.Events.FirstOrDefault((activityEvent) => activityEvent.Name == FeatureFlagActivityEventName);

                    if (evaluationEvent.HasValue && evaluationEvent.Value.Tags.Any())
                    {
                        HandleFeatureFlagEvent(evaluationEvent.Value);
                    }
                }
            };

            ActivitySource.AddActivityListener(_activityListener);
        }

        /// <summary>
        /// Disposes the resources used by the <see cref="OpenTelemetryEventPublisher"/>.
        /// </summary>
        public void Dispose()
        {
            _activityListener.Dispose();
        }

        private void HandleFeatureFlagEvent(ActivityEvent activityEvent)
        {
            var state = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>(AzureMonitorCustomEventNameKey, FeatureEvaluationEventName)
            };

            foreach (KeyValuePair<string, object> tag in activityEvent.Tags)
            {
                // The custom event name key is reserved: it must always be "FeatureEvaluation" and
                // cannot be overridden by feature flag telemetry metadata tags.
                if (tag.Key == AzureMonitorCustomEventNameKey)
                {
                    _logger.LogWarning($"{tag.Key} from telemetry metadata will be ignored, as it would override a reserved key.");

                    continue;
                }

                // FeatureEvaluation event schema: https://github.com/microsoft/FeatureManagement/blob/main/Schema/FeatureEvaluationEvent/FeatureEvaluationEvent.v1.0.0.schema.json
                if (tag.Value is VariantAssignmentReason reason)
                {
                    string reasonValue;

                    switch (reason)
                    {
                        case VariantAssignmentReason.None:
                            reasonValue = "None";
                            break;
                        case VariantAssignmentReason.DefaultWhenDisabled:
                            reasonValue = "DefaultWhenDisabled";
                            break;
                        case VariantAssignmentReason.DefaultWhenEnabled:
                            reasonValue = "DefaultWhenEnabled";
                            break;
                        case VariantAssignmentReason.User:
                            reasonValue = "User";
                            break;
                        case VariantAssignmentReason.Group:
                            reasonValue = "Group";
                            break;
                        case VariantAssignmentReason.Percentile:
                            reasonValue = "Percentile";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(activityEvent), "The variant assignment reason is unrecognizable.");
                    }

                    state.Add(new KeyValuePair<string, object>(tag.Key, reasonValue));
                }
                else
                {
                    state.Add(new KeyValuePair<string, object>(tag.Key, tag.Value));
                }
            }

            _logger.Log(
                LogLevel.Information,
                new EventId(0, FeatureEvaluationEventName),
                state,
                null,
                (s, ex) => FeatureEvaluationEventName);
        }
    }
}
