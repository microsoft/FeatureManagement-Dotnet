﻿// Copyright (c) Microsoft Corporation.
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
        public Func<FeatureFilterEvaluationContext, bool> Callback { get; set; }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(Callback?.Invoke(context) ?? false);
        }
    }
}
