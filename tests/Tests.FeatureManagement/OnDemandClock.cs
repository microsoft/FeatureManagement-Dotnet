// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

using System;
using Microsoft.FeatureManagement.FeatureFilters;

namespace Tests.FeatureManagement
{
    class OnDemandClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}
