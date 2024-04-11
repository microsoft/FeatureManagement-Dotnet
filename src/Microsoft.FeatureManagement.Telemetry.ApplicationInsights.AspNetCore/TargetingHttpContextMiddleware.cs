// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.FeatureFilters;

namespace Microsoft.FeatureManagement.Telemetry.ApplicationInsights.AspNetCore
{
    /// <summary>
    /// Used to add targeting information to HTTP context.
    /// </summary>
    public class TargetingHttpContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates an instance of the TargetingHttpContextMiddleware
        /// </summary>
        public TargetingHttpContextMiddleware(RequestDelegate next, ILoggerFactory loggerFactory) {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = loggerFactory?.CreateLogger<TargetingHttpContextMiddleware>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Adds targeting information to the HTTP context.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> to add the targeting information to.</param>
        /// <param name="targetingContextAccessor">The <see cref="ITargetingContextAccessor"/> to retrieve the targeting information from.</param>
        /// <exception cref="ArgumentNullException">Thrown if the provided context or targetingContextAccessor is null.</exception>
        public async Task InvokeAsync(HttpContext context, ITargetingContextAccessor targetingContextAccessor)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (targetingContextAccessor == null)
            {
                throw new ArgumentNullException(nameof(targetingContextAccessor));
            }

            TargetingContext targetingContext = await targetingContextAccessor.GetContextAsync().ConfigureAwait(false);

            if (targetingContext != null)
            {
                var requestTelemetry = context.Features.Get<RequestTelemetry>();

                if (requestTelemetry != null)
                {
                    requestTelemetry.Properties.Add(Constants.TargetingIdKey, targetingContext.UserId);
                }
            }
            else
            {
                _logger.LogDebug("The targeting context accessor returned a null TargetingContext");
            }

            await _next(context);
        }
    }
}
