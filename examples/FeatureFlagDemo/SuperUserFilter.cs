// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using System.Threading.Tasks;

namespace FeatureFlagDemo.FeatureManagement.FeatureFilters
{
    public class SuperUserFilter : IFeatureFilter
    {
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            return Task.FromResult(false);
        }
    }
}
