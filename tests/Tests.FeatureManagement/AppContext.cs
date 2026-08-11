// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.FeatureFilters;
using System.Collections.Generic;

namespace Tests.FeatureManagement
{
    class AppContext : IAccountContext, ITargetingContext
    {
        public string AccountId { get; set; }

        public string UserId { get; set; }

        public IEnumerable<string> Groups { get; set; }
    }
}
