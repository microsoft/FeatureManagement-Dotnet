// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;

namespace Tests.FeatureManagement
{
    interface IAccountContext : IFeatureFilterContext
    {
        string AccountId { get; }
    }
}
