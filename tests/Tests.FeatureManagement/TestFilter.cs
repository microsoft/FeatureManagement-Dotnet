// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    class TestFilter : IFeatureFilter
    {
        public Func<FeatureFilterEvaluationContext, Task<bool>> Callback { get; set; }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, CancellationToken cancellationToken)
        {
            return Callback?.Invoke(context) ?? Task.FromResult(false);
        }
    }

    //
    // Offers the same functionality as TestFilter, but allows a feature used in tests to reference two different filters
    class Test2Filter : TestFilter
    {
    }
}
