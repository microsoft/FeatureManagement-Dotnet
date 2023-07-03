// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// Provides access to the current targeting context.
    /// </summary>
    public interface ITargetingContextAccessor
    {
        /// <summary>
        /// Retrieves the current targeting context.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The current targeting context.</returns>
        ValueTask<TargetingContext> GetContextAsync(CancellationToken cancellationToken);
    }
}
