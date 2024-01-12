// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Threading.Tasks;

namespace EvaluationDataToApplicationInsights.Telemetry
{
    /// <summary>
    /// Used to add targeting information to http context. This allows synronous code to access targeting information.
    /// </summary>
    public class TargetingHttpContextMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Creates an instance of the TargetingHttpContextMiddleware
        /// </summary>
        public TargetingHttpContextMiddleware(RequestDelegate next) { 
            _next = next; 
        }

        /// <summary>
        /// Adds targeting information to the http context.
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
                context.Items["TargetingId"] = targetingContext.UserId;
            }

            await _next(context);
        }
    }
}
