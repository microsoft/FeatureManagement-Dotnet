// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.AspNetCore
{

    /// <summary>
    /// An endpoint filter that requires a feature flag to be enabled.
    /// </summary>
    internal class FeatureFlagsEndpointFilter : IEndpointFilter
    {
        public string FeatureName { get; }

        /// <summary>
        /// Creates a new instance of <see cref="FeatureFlagsEndpointFilter"/>.
        /// </summary>
        /// <param name="featureName">The name of the feature flag to evaluate for this endpoint.</param>
        public FeatureFlagsEndpointFilter(string featureName)
        {
            if (string.IsNullOrEmpty(featureName))
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            FeatureName = featureName;
        }

        /// <summary>
        /// Invokes the feature flag filter to control endpoint access based on feature state.
        /// </summary>
        /// <param name="context">The endpoint filter invocation context containing the current HTTP context.</param>
        /// <param name="next">The delegate representing the next filter in the pipeline.</param>
        /// <returns>
        /// A <see cref="NotFound"/> if the feature is disabled, otherwise continues the pipeline by calling the next delegate.
        /// Returns a ValueTask containing the result object.
        /// </returns>
        public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            IVariantFeatureManagerSnapshot fm = context.HttpContext.RequestServices.GetRequiredService<IVariantFeatureManagerSnapshot>();

            return await fm.IsEnabledAsync(FeatureName, context.HttpContext.RequestAborted) ? await next(context) : Results.NotFound();
        }
    }
}
