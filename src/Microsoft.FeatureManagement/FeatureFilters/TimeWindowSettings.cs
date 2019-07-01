// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;

namespace Microsoft.FeatureManagement.FeatureFilters
{
    class TimeWindowSettings
    {
        public DateTimeOffset? Start { get; set; } // E.g. "Wed, 01 May 2019 22:59:30 GMT"

        public DateTimeOffset? End { get; set; } // E.g. "Wed, 01 May 2019 23:00:00 GMT"
    }
}
