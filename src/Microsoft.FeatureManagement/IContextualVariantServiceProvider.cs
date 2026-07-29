// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to get different implementation variants of TService, optionally providing a context that can be used to evaluate contextual feature filters.
    /// </summary>
    public interface IContextualVariantServiceProvider<TService> : IVariantServiceProvider<TService> where TService : class
    {
        /// <summary>
        /// Gets an implementation variant of TService, using the provided context to evaluate contextual feature filters when determining the assigned variant.
        /// </summary>
        /// <typeparam name="TContext">The type of the context.</typeparam>
        /// <param name="context">A context that provides information used to evaluate contextual feature filters and to determine which variant will be assigned.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An implementation of TService.</returns>
        ValueTask<TService> GetServiceAsync<TContext>(TContext context, CancellationToken cancellationToken);
    }
}
