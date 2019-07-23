// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;

namespace Consoto.Banking.AccountServer.FeatureFilters
{
    public interface IAccountContext : IFeatureFilterContext
    {
        string AccountId { get; }
    }
}
