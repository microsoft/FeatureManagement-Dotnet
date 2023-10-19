// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.FeatureFilters;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    internal class EmptyTargetingContextAccessor : ITargetingContextAccessor
    {
        public ValueTask<TargetingContext> GetContextAsync()
        {
            TargetingContext targetingContext = new TargetingContext
            {
                UserId = "",
                Groups = new List<string>()
            };

            return new ValueTask<TargetingContext>(targetingContext);
        }
    }
}