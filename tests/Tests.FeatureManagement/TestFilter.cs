// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using System;

namespace Tests.FeatureManagement
{
    class TestFilter : IFeatureFilter
    {
        public Func<FeatureFilterEvaluationContext, bool> Callback { get; set; }

        public bool Evaluate(FeatureFilterEvaluationContext context)
        {
            return Callback?.Invoke(context) ?? false;
        }
    }
}
