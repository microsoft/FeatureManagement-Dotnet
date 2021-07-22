﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Empty implementation of <see cref="ISessionManager"/>.
    /// </summary>
    class EmptySessionManager : ISessionManager
    {
        public Task SetAsync(string featureName, bool enabled, CancellationToken _)
        {
            return Task.CompletedTask;
        }

        public Task<bool?> GetAsync(string featureName, CancellationToken _)
        {
            return Task.FromResult((bool?)null);
        }
    }
}
