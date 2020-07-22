// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.FeatureManagement;

namespace Consoto.Banking.AccountService.FeatureFilters
{
    public interface IAccountContext : IFeatureContext
    {
        string AccountId { get; }
    }
}
