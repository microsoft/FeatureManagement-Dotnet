// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

/* Unmerged change from project 'Microsoft.FeatureManagement.AspNetCore(net7.0)'
Before:
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
After:
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.
*/

/* Unmerged change from project 'Microsoft.FeatureManagement.AspNetCore(net8.0)'
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

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.FeatureManagement.Mvc
{
    /// <summary>
    /// A default disabled feature handler that performs a minimal amount of work for disabled feature requests.
    /// </summary>
    class NotFoundDisabledFeaturesHandler : IDisabledFeaturesHandler
    {
        public Task HandleDisabledFeatures(IEnumerable<string> features, ActionExecutingContext context)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status404NotFound);

            return Task.CompletedTask;
        }
    }
}
