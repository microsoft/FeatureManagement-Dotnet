// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Microsoft.FeatureManagement.Telemetry.AzureMonitor
{
    /// <summary>
    /// Listens to <see cref="Activity"/> events from feature management and sends them to Azure Monitor via structured logging.
    /// </summary>
    internal sealed class AzureMonitorEventPublisher : IDisposable
    {
        private readonly ILogger<AzureMonitorEventPublisher> _logger;
        private readonly ActivityListener _activityListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureMonitorEventPublisher"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public AzureMonitorEventPublisher(ILogger<AzureMonitorEventPublisher> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _activityListener = new ActivityListener
            {
                ShouldListenTo = (activitySource) => activitySource.Name == "Microsoft.FeatureManagement",
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
                ActivityStopped = (activity) =>
                {
                    ActivityEvent? evaluationEvent = activity.Events.FirstOrDefault((activityEvent) => activityEvent.Name == "FeatureFlag");

                    if (evaluationEvent.HasValue && evaluationEvent.Value.Tags.Any())
                    {
                        HandleFeatureFlagEvent(evaluationEvent.Value);
                    }
                }
            };

            ActivitySource.AddActivityListener(_activityListener);
        }

        /// <summary>
        /// Disposes the resources used by the <see cref="AzureMonitorEventPublisher"/>.
        /// </summary>
        public void Dispose()
        {
            _activityListener.Dispose();
        }

        private void HandleFeatureFlagEvent(ActivityEvent activityEvent)
        {
            var properties = new Dictionary<string, object>();

            foreach (var tag in activityEvent.Tags)
            {
                // FeatureEvaluation event schema: https://github.com/microsoft/FeatureManagement/blob/main/Schema/FeatureEvaluationEvent/FeatureEvaluationEvent.v1.0.0.schema.json
                if (tag.Value is VariantAssignmentReason reason)
                {
                    switch (reason)
                    {
                        case VariantAssignmentReason.None:
                            properties[tag.Key] = "None";
                            break;
                        case VariantAssignmentReason.DefaultWhenDisabled:
                            properties[tag.Key] = "DefaultWhenDisabled";
                            break;
                        case VariantAssignmentReason.DefaultWhenEnabled:
                            properties[tag.Key] = "DefaultWhenEnabled";
                            break;
                        case VariantAssignmentReason.User:
                            properties[tag.Key] = "User";
                            break;
                        case VariantAssignmentReason.Group:
                            properties[tag.Key] = "Group";
                            break;
                        case VariantAssignmentReason.Percentile:
                            properties[tag.Key] = "Percentile";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(activityEvent), "The variant assignment reason is unrecognizable.");
                    }
                }
                else if (tag.Value is bool val)
                {
                    properties[tag.Key] = val ? "True" : "False";
                }
                else
                {
                    properties[tag.Key] = tag.Value?.ToString();
                }
            }

            _logger.LogFeatureEvaluation("FeatureEvaluation", properties);
        }
    }
}
