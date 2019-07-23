// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Consoto.Banking.AccountServer.FeatureFilters;
using Microsoft.FeatureManagement;

namespace Consoto.Banking.AccountServer
{
    class AccountServerContext : IAccountContext, IFeatureFilterContext
    {
        public string AccountId { get; set; }
    }
}
