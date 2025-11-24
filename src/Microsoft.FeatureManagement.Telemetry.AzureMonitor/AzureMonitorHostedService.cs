// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.FeatureManagement.Telemetry.AzureMonitor
{
    /// <summary>
    /// A hosted service used to construct and dispose the <see cref="AzureMonitorEventPublisher"/>
    /// </summary>
    internal sealed class AzureMonitorHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureMonitorHostedService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to get the publisher from.</param>
        public AzureMonitorHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Uses the service provider to construct a <see cref="AzureMonitorEventPublisher"/> which will start listening for activities.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _serviceProvider.GetRequiredService<AzureMonitorEventPublisher>();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops this hosted service.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
