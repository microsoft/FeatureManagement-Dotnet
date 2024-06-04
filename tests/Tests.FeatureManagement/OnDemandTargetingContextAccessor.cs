// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

using System.Threading.Tasks;
using Microsoft.FeatureManagement.FeatureFilters;

namespace Tests.FeatureManagement
{
    class OnDemandTargetingContextAccessor : ITargetingContextAccessor
    {
        public TargetingContext Current { get; set; }

        public ValueTask<TargetingContext> GetContextAsync()
        {
            return new ValueTask<TargetingContext>(Current);
        }
    }
}
