// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

/* Unmerged change from project 'Tests.FeatureManagement(net6.0)'
Before:
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
After:
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.
*/

/* Unmerged change from project 'Tests.FeatureManagement(net7.0)'
Before:
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
After:
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.
*/

/* Unmerged change from project 'Tests.FeatureManagement(net8.0)'
Before:
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
After:
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.
*/
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
