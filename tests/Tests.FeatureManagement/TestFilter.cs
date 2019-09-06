// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    class TestFilter : IFeatureFilter, IContextualFeatureFilter<IAccountContext>
    {
        public Func<FeatureFilterEvaluationContext, bool> Callback { get; set; }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            return Task.FromResult(Callback?.Invoke(context) ?? false);
        }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureFilterEvaluationContext, IAccountContext accountContext)
        {
            if (string.IsNullOrEmpty(accountContext?.AccountId))
            {
                throw new ArgumentNullException(nameof(accountContext.AccountId));
            }

            var allowedAccounts = new List<string>();

            featureFilterEvaluationContext.Parameters.Bind("AllowedAccounts", allowedAccounts);

            return Task.FromResult(allowedAccounts.Contains(accountContext.AccountId));
        }
    }
}
