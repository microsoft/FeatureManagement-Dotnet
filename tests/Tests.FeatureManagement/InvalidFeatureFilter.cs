﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading.Tasks;
using Microsoft.FeatureManagement;

namespace Tests.FeatureManagement
{
    //
    // Cannot implement more than one IFeatureFilter interface
    internal class InvalidFeatureFilter : IContextualFeatureFilter<IAccountContext>, IContextualFeatureFilter<object>
    {
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, IAccountContext accountContext)
        {
            return Task.FromResult(false);
        }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureFilterContext, object appContext)
        {
            return Task.FromResult(false);
        }
    }

    //
    // Cannot implement more than one IFeatureFilter interface
    internal class InvalidFeatureFilter2 : IFeatureFilter, IContextualFeatureFilter<object>
    {
        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureFilterContext, object appContext)
        {
            return Task.FromResult(false);
        }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            return Task.FromResult(false);
        }
    }
}
