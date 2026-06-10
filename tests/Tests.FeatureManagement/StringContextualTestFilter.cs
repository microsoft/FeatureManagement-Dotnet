// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using System;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    [FilterAlias("Test")]
    class StringContextualTestFilter : IContextualFeatureFilter<string>
    {
        public Func<FeatureFilterEvaluationContext, string, bool> ContextualCallback { get; set; }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, string stringContext)
        {
            return Task.FromResult(ContextualCallback?.Invoke(context, stringContext) ?? false);
        }
    }
}
