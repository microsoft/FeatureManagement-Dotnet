// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to get different implementation variants of TService.
    /// </summary>
    public interface IVariantServiceProvider<TService> where TService : class
    {
        /// <summary>
        /// Gets an implementation variant of TService.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An implementation of TService.</returns>
        ValueTask<TService> GetAsync(CancellationToken cancellationToken);
    }
}
