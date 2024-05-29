// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

/* Unmerged change from project 'Tests.FeatureManagement(net6.0)'
Before:
using Microsoft.FeatureManagement.FeatureFilters;
After:
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

using Microsoft.FeatureManagement.FeatureFilters;
*/

/* Unmerged change from project 'Tests.FeatureManagement(net7.0)'
Before:
using Microsoft.FeatureManagement.FeatureFilters;
After:
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

using Microsoft.FeatureManagement.FeatureFilters;
*/

/* Unmerged change from project 'Tests.FeatureManagement(net8.0)'
Before:
using Microsoft.FeatureManagement.FeatureFilters;
After:
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

using Microsoft.FeatureManagement.FeatureFilters;
*/
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
