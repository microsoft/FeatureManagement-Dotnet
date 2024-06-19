// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Threading.Tasks;
using Microsoft.FeatureManagement;

namespace Tests.FeatureManagement
{
    class ContextualTestFilter : IContextualFeatureFilter<IAccountContext>
    {
        public Func<FeatureFilterEvaluationContext, IAccountContext, bool> ContextualCallback { get; set; }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, IAccountContext accountContext)
        {
            return Task.FromResult(ContextualCallback?.Invoke(context, accountContext) ?? false);
        }
    }
}
