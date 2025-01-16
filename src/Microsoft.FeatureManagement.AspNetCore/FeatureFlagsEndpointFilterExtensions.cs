using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.AspNetCore
{

    /// <summary>
    /// Extension methods that provide feature management integration for ASP.NET Core endpoint building.
    /// </summary>
    public static class FeatureFlagsEndpointFilterExtensions
    {
        /// <summary>
        /// Adds a feature flag filter to the endpoint that controls access based on feature state.
        /// </summary>
        /// <param name="builder">The endpoint convention builder.</param>
        /// <param name="featureName">The name of the feature flag to evaluate.</param>
        /// <typeparam name="TBuilder">The type of the endpoint convention builder.</typeparam>
        /// <returns>The endpoint convention builder for chaining.</returns>
        /// <remarks>
        /// This extension method enables feature flag control over endpoint access. When the feature is disabled,
        /// requests to the endpoint will return a 404 Not Found response. The targeting context is obtained
        /// from the ITargetingContextAccessor registered in the service collection.
        /// </remarks>
        /// <example>
        /// <code>
        /// endpoints.MapGet("/api/feature", () => "Feature Enabled")
        ///     .WithFeatureGate("MyFeature");
        /// </code>
        /// </example>
        public static TBuilder WithFeatureGate<TBuilder>(this TBuilder builder, string featureName)
            where TBuilder : IEndpointConventionBuilder
        {
            return builder.AddEndpointFilter(new FeatureFlagsEndpointFilter(featureName));
        }
    }

    /// <summary>
    /// An endpoint filter that requires a feature flag to be enabled.
    /// </summary>
    public class FeatureFlagsEndpointFilter : IEndpointFilter
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
        /// A <see cref="Result.NotFound"/> if the feature is disabled, otherwise continues the pipeline by calling the next delegate.
        /// Returns a ValueTask containing the result object.
        /// </returns>
        public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            IFeatureManagerSnapshot fm = context.HttpContext.RequestServices.GetRequiredService<IFeatureManagerSnapshot>();
            if (fm is null)
            {
                return await next(context);
            }

            bool enabled = await fm.IsEnabledAsync(FeatureName);
            return enabled ? await next(context) : Results.NotFound();
        }
    }
}
