// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to add targeting information to HTTP context.
    /// </summary>
    public class TargetingHttpContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        private const string TargetingIdKey = "TargetingId";

        /// <summary>
        /// Creates an instance of the TargetingHttpContextMiddleware
        /// </summary>
        public TargetingHttpContextMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = loggerFactory?.CreateLogger<TargetingHttpContextMiddleware>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Adds targeting information to the HTTP context.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> to add the targeting information to.</param>
        /// <param name="targetingContextAccessor">The <see cref="ITargetingContextAccessor"/> to retrieve the targeting information from.</param>
        /// <exception cref="ArgumentNullException">Thrown if the provided context or targetingContextAccessor is null.</exception>
        public async Task InvokeAsync(HttpContext httpContext, ITargetingContextAccessor targetingContextAccessor)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (targetingContextAccessor == null)
            {
                throw new ArgumentNullException(nameof(targetingContextAccessor));
            }

            TargetingContext targetingContext = await targetingContextAccessor.GetContextAsync().ConfigureAwait(false);

            if (targetingContext != null)
            {
                var activityFeature = httpContext.Features.Get<IHttpActivityFeature>();

                if (activityFeature == null)
                {
                    _logger.LogWarning("A request feature required for telemetry, IHttpActivityFeature, was not found in the request's feature collection. Removing this feature from the request's feature collection will cause telemetry emission to fail.");
                }
                else if (activityFeature.Activity == null)
                {
                    _logger.LogWarning("A request feature required for telemetry, IHttpActivityFeature, has a null Activity property. If you have updated IHttpActivityFeature, ensure the Activity property is not null. A missing Activity will result in telemetry emission failures.");
                }
                else
                {
                    activityFeature.Activity.AddBaggage(TargetingIdKey, targetingContext.UserId);
                }
            }
            else
            {
                _logger.LogDebug("The targeting context accessor returned a null TargetingContext");
            }

            await _next(httpContext);
        }
    }
}
