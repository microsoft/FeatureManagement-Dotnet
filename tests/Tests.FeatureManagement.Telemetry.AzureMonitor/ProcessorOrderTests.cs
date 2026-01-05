using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Telemetry.AzureMonitor;
using OpenTelemetry;
using OpenTelemetry.Trace;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace Tests.FeatureManagement.Telemetry.AzureMonitor
{
    public class ProcessorOrderTests
    {
        [Fact]
        public void TargetingProcessor_RunsBefore_Exporter()
        {
            var services = new ServiceCollection();

            // List to capture exported spans
            var exportedItems = new List<Activity>();
            var exporter = new InMemoryExporter(exportedItems);

            // 1. User adds OpenTelemetry and Exporter
            // Note: In a real app, UseAzureMonitor() would add the exporter.
            // We simulate this by adding a processor that acts as an exporter.
            var exportProcessor = new SimpleActivityExportProcessor(exporter);
            services.AddOpenTelemetry()
                .WithTracing(builder => builder
                    .AddSource("TestTracer")
                    .AddProcessor(exportProcessor));

            // 2. User adds FeatureManagement and AzureMonitorTelemetry
            services.AddFeatureManagement()
                .AddAzureMonitorTelemetry();

            using var serviceProvider = services.BuildServiceProvider();
            var tracerProvider = serviceProvider.GetRequiredService<TracerProvider>();

            // 3. Start Activity with Baggage
            using var source = new ActivitySource("TestTracer");
            using (var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            })
            {
                ActivitySource.AddActivityListener(listener);

                using (var activity = source.StartActivity("TestActivity"))
                {
                    activity.AddBaggage("TargetingId", "User123");

                    // End activity to trigger processors
                }
            }

            // 4. Verify Exporter received the tag
            Assert.Single(exporter.ExportedTags);
            var tags = exporter.ExportedTags[0];

            // If TargetingProcessor ran first, the tag should be present.
            Assert.Contains(tags, t => t.Key == "TargetingId" && t.Value == "User123");

            exportProcessor.Dispose();
        }

        private class InMemoryExporter : BaseExporter<Activity>
        {
            private readonly List<Activity> _exportedItems;
            public readonly List<IEnumerable<KeyValuePair<string, string>>> ExportedTags = new List<IEnumerable<KeyValuePair<string, string>>>();

            public InMemoryExporter(List<Activity> exportedItems)
            {
                _exportedItems = exportedItems;
            }

            public override ExportResult Export(in Batch<Activity> batch)
            {
                foreach (var activity in batch)
                {
                    _exportedItems.Add(activity);
                    // Capture tags at the moment of export
                    ExportedTags.Add(activity.Tags.Select(t => new KeyValuePair<string, string>(t.Key, t.Value)).ToList());
                }

                return ExportResult.Success;
            }
        }
    }
}
