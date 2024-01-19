// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    public interface IVariantServiceProvider<TService> where TService : class
    {
        ValueTask<TService> GetAsync(CancellationToken cancellationToken);
    }
}
