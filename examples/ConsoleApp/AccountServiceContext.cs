// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Consoto.Banking.AccountService.FeatureFilters;

namespace Consoto.Banking.AccountService
{
    class AccountServiceContext : IAccountContext
    {
        public string AccountId { get; set; }
    }
}
