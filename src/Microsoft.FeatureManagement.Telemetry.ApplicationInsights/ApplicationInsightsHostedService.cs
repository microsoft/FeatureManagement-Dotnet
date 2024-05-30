using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;

namespace Microsoft.FeatureManagement.Telemetry.ApplicationInsights
{
    /// <summary>
    /// A hosted service used to construct and dispose the <see cref="ApplicationInsightsEventPublisher"/>
    /// </summary>
    internal class ApplicationInsightsHostedService : IHostedService
    {
        private readonly TelemetryClient _telemetryClient;
        private ApplicationInsightsEventPublisher appInsightsPublisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsHostedService"/> class.
        /// </summary>
        /// <param name="telemetryClient">The <see cref="TelemetryClient"/> instance used for telemetry.</param>
        public ApplicationInsightsHostedService(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        }

        /// <summary>
        /// Constructs the <see cref="ApplicationInsightsEventPublisher"/> which will start listening for events.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            appInsightsPublisher = new ApplicationInsightsEventPublisher(_telemetryClient);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes of the <see cref="ApplicationInsightsEventPublisher"/>.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            appInsightsPublisher.Dispose();
            appInsightsPublisher = null;

            return Task.CompletedTask;
        }
    }
}
