using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to get TService implementation based on the feature status.
    /// </summary>
    public interface IFeatureServiceProvider<TService> where TService : class
    {
        /// <summary>
        /// Gets an implementation of TService.
        /// </summary>
        /// <returns>An implementation of TService.</returns>
        ValueTask<TService> GetServiceAsync();

        /// <summary>
        /// Gets an implementation of TService with additional usage of the feature filters.
        /// </summary>
        /// <param name="context">A context that provides information to evaluate whether a feature should be on or off.</param>
        /// <returns>An implementation of TService.</returns>
        ValueTask<TService> GetServiceAsync<TContext>(TContext context);
    }
}
