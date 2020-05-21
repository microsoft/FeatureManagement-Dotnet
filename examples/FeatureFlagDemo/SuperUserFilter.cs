// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using System.Threading.Tasks;
using Microsoft.FeatureManagement;

namespace FeatureFlagDemo
{
	public class SuperUserFilter : IFeatureFilter
    {
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            return Task.FromResult(false);
        }
    }
}
