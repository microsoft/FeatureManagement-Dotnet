using Microsoft.ApplicationInsights;
using OpenTelemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.FeatureManagement.Telemetry.ApplicationInsights
{
    

    internal class EvaluationEventExporter : BaseExporter<Activity>
    {
        private readonly TelemetryClient _telemetryClient;

        public EvaluationEventExporter(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            /*FeatureManager.ActivitySource. = new ActivityListener
            {
                ShouldListenTo = (activity) => activity.Name == "Microsoft.FeatureManagement",
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
                ActivityStopped = (activity) =>
                {
                    ActivityEvent? evaluationEvent = activity.Events.FirstOrDefault((activityEvent) => activityEvent.Name == "feature_flag");
                    
                    if (evaluationEvent != null && evaluationEvent.Value.Tags.Any())
                    {
                        var properties = new Dictionary<string, string>();
                        foreach (var tag in activity.Tags)
                        {
                            properties[tag.Key] = tag.Value;
                        }
                        _telemetryClient.TrackEvent("FeatureEvaluation", properties);
                    }
                }
            };*/
        }

        public override ExportResult Export(in Batch<Activity> batch)
        {
            ExportResult exportResult = ExportResult.Failure;

            try
            {
                foreach (Activity activity in batch)
                {
                    ActivityEvent? evaluationEvent = activity.Events.FirstOrDefault((activityEvent) => activityEvent.Name == "feature_flag");

                    if (evaluationEvent != null && evaluationEvent.Value.Tags.Any())
                    {
                        var properties = new Dictionary<string, string>();
                        foreach (var tag in activity.Tags)
                        {
                            properties[tag.Key] = tag.Value;
                        }
                        _telemetryClient.TrackEvent("FeatureEvaluation", properties);
                    }
                }

                exportResult = ExportResult.Success;
            } catch (Exception ex)
            {
                // Do something
            }

            return exportResult;
        }
    }
}
