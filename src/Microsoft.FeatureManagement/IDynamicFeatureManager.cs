// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to access the variants of a dynamic feature.
    /// </summary>
    public interface IDynamicFeatureManager
    {
        /// <summary>
        /// Retrieves a list of dynamic feature names registered in the feature manager.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An enumerator which provides asynchronous iteration over the dynamic feature names registered in the feature manager.</returns>
        IAsyncEnumerable<string> GetDynamicFeatureNamesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a typed representation of the feature variant that should be used for a given dynamic feature.
        /// </summary>
        /// <typeparam name="T">The type that the feature variant's configuration should be bound to.</typeparam>
        /// <param name="dynamicFeature">The name of the dynamic feature.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A typed representation of the feature variant that should be used for a given dynamic feature.</returns>
        ValueTask<T> GetVariantAsync<T>(string dynamicFeature, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a typed representation of the feature variant that should be used for a given dynamic feature.
        /// </summary>
        /// <typeparam name="T">The type that the feature variant's configuration should be bound to.</typeparam>
        /// <typeparam name="TContext">The type of the context being provided to the dynamic feature manager for use during the process of choosing which variant to use.</typeparam>
        /// <param name="dynamicFeature">The name of the dynamic feature.</param>
        /// <param name="context">A context providing information that can be used to evaluate which variant should be used for the dynamic feature.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A typed representation of the feature variant's configuration that should be used for a given feature.</returns>
        ValueTask<T> GetVariantAsync<T, TContext>(string dynamicFeature, TContext context, CancellationToken cancellationToken = default);
    }
}
