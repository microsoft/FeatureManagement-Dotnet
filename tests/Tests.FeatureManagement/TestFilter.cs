// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using System;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    class TestFilter : IFeatureFilter, IContextualFeatureFilter<IAccountContext>
    {
        public Func<FeatureFilterEvaluationContext, bool> Callback { get; set; }

        public Func<FeatureFilterEvaluationContext, IAccountContext, bool> ContextualCallback { get; set; }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            return Task.FromResult(Callback?.Invoke(context) ?? false);
        }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, IAccountContext accountContext)
        {
            return Task.FromResult(ContextualCallback?.Invoke(context, accountContext) ?? false);
        }
    }
}
