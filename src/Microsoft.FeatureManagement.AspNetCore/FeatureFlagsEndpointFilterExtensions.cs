using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Threading.Tasks;

#if NET7_0_OR_GREATER
namespace Microsoft.FeatureManagement.AspNetCore;

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
    /// <param name="predicate">A function that provides the targeting context for feature evaluation.</param>
    /// <typeparam name="TBuilder">The type of the endpoint convention builder.</typeparam>
    /// <returns>The endpoint convention builder for chaining.</returns>
    /// <remarks>
    /// This extension method enables feature flag control over endpoint access. When the feature is disabled,
    /// requests to the endpoint will return a 404 Not Found response. The targeting context from the predicate
    /// is used to evaluate the feature state for the current request.
    /// </remarks>
    /// <example>
    /// <code>
    /// endpoints.MapGet("/api/feature", () => "Feature Enabled")
    ///     .WithFeatureFlag("MyFeature", () => new TargetingContext
    ///     {
    ///         UserId = "user123",
    ///         Groups = new[] { "beta-testers" }
    ///     });
    /// </code>
    /// </example>
    public static TBuilder WithFeatureFlag<TBuilder>(this TBuilder builder,
        string featureName,
        Func<TargetingContext> predicate) where TBuilder : IEndpointConventionBuilder
    {
        return builder.AddEndpointFilter(new FeatureFlagsEndpointFilter(featureName, predicate));
    }
}

/// <summary>
/// An endpoint filter that requires a feature flag to be enabled.
/// </summary>
public class FeatureFlagsEndpointFilter : IEndpointFilter
{
    private readonly string _featureName;
    private readonly Func<TargetingContext> _predicate;
    /// <summary>
    /// Creates a new instance of <see cref="FeatureFlagsEndpointFilter"/>.
    /// </summary>
    /// <param name="featureName">The name of the feature flag to evaluate for this endpoint.</param>
    /// <param name="predicate">A function that provides the targeting context for feature evaluation.</param>
    /// <exception cref="ArgumentNullException">Thrown when featureName or predicate is null.</exception>
    public FeatureFlagsEndpointFilter(string featureName, Func<TargetingContext> predicate)
    {
        _featureName = featureName;
        _predicate = predicate;
    }

    /// <summary>
    /// Invokes the feature flag filter to control endpoint access based on feature state.
    /// </summary>
    /// <param name="context">The endpoint filter invocation context containing the current HTTP context.</param>
    /// <param name="next">The delegate representing the next filter in the pipeline.</param>
    /// <returns>
    /// A NotFound result if the feature is disabled, otherwise continues the pipeline by calling the next delegate.
    /// Returns a ValueTask containing the result object.
    /// </returns>
    /// <remarks>
    /// The filter retrieves the IFeatureManager from request services and evaluates the feature flag.
    /// If the feature manager is not available, the filter allows the request to proceed.
    /// For disabled features, returns a 404 Not Found response instead of executing the endpoint.
    /// </remarks>
    public async ValueTask<object> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var featureManager = context.HttpContext.RequestServices.GetRequiredService<IFeatureManager>();
        if (featureManager is null)
            return await next(context);

        var featureFlag = await featureManager.IsEnabledAsync(_featureName, _predicate);
        return !featureFlag ? Results.NotFound() : await next(context);
    }
}
#endif
