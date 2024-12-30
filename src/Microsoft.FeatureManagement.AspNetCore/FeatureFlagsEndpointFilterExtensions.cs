using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Threading.Tasks;

#if NET7_0_OR_GREATER
namespace Microsoft.FeatureManagement.AspNetCore;

/// <summary>
/// Extension methods that provide feature management integration for ASP.NET Core application building.
/// </summary>
public static class FeatureFlagsEndpointFilterExtensions
{
    /// <summary>
    /// Adds a feature flag filter to the endpoint.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="featureName"></param>
    /// <param name="predicate"></param>
    /// <typeparam name="TBuilder"></typeparam>
    /// <returns></returns>
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
    /// <param name="featureName"></param>
    /// <param name="predicate"></param>
    public FeatureFlagsEndpointFilter(string featureName, Func<TargetingContext> predicate)
    {
        _featureName = featureName;
        _predicate = predicate;
    }

    /// <summary>
    /// Invokes the feature flag filter.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
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
