// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    //
    // Cannot implement more than one IFeatureFilter interface
    class InvalidFeatureFilter : IContextualFeatureFilter<IAccountContext>, IContextualFeatureFilter<IFeatureContext>
    {
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, IAccountContext accountContext)
        {
            return Task.FromResult(false);
        }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureFilterContext, IFeatureContext featureContext)
        {
            return Task.FromResult(false);
        }
    }

    //
    // Cannot implement more than one IFeatureFilter interface
    class InvalidFeatureFilter2 : IFeatureFilter, IContextualFeatureFilter<IFeatureContext>
    {
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureFilterContext, IFeatureContext featureContext)
        {
            return Task.FromResult(false);
        }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            return Task.FromResult(false);
        }
    }
}
