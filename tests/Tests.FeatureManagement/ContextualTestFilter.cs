// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using System;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    class ContextualTestFilter : IContextualFeatureFilter<AppContext>
    {
        public Func<FeatureFilterEvaluationContext, AppContext, bool> ContextualCallback { get; set; }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, AppContext accountContext)
        {
            return Task.FromResult(ContextualCallback?.Invoke(context, accountContext) ?? false);
        }
    }
}
