// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.FeatureManagement;

namespace Tests.FeatureManagement
{
    interface IAccountContext : IFeatureContext
    {
        string AccountId { get; }
    }
}
