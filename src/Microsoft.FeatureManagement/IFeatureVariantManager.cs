// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to access the variants of a feature.
    /// </summary>
    public interface IFeatureVariantManager
    {
        /// <summary>
        /// Retrieves a typed representation of the configuration variant that should be used for a given feature.
        /// </summary>
        /// <typeparam name="T">The type that the variants configuration should be bound to.</typeparam>
        /// <param name="feature">The name of the feature.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A typed representation of the configuration variant that should be used for a given feature.</returns>
        ValueTask<T> GetVariantAsync<T>(string feature, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a typed representation of the configuration variant that should be used for a given feature.
        /// </summary>
        /// <typeparam name="T">The type that the variants configuration should be bound to.</typeparam>
        /// <typeparam name="TContext">The type of the context being provided to the feature variant manger for use during the process of choosing which variant to use.</typeparam>
        /// <param name="feature">The name of the feature.</param>
        /// <param name="context">A context providing information that can be used to evaluate whether a feature should be on or off.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A typed representation of the configuration variant that should be used for a given feature.</returns>
        ValueTask<T> GetVariantAsync<T, TContext>(string feature, TContext context, CancellationToken cancellationToken);
    }
}
