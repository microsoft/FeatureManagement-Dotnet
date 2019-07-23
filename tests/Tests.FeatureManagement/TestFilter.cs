// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using System;
using System.Collections.Generic;

namespace Tests.FeatureManagement
{
    class TestFilter : IFeatureFilter, IContextualFeatureFilter<IAccountContext>
    {
        public Func<FeatureFilterEvaluationContext, bool> Callback { get; set; }

        public bool Evaluate(FeatureFilterEvaluationContext context)
        {
            return Callback?.Invoke(context) ?? false;
        }

        public bool Evaluate(FeatureFilterEvaluationContext featureFilterEvaluationContext, IAccountContext accountContext)
        {
            if (string.IsNullOrEmpty(accountContext?.AccountId))
            {
                throw new ArgumentNullException(nameof(accountContext.AccountId));
            }

            var allowedAccounts = new List<string>();

            featureFilterEvaluationContext.Parameters.Bind("AllowedAccounts", allowedAccounts);

            return allowedAccounts.Contains(accountContext.AccountId);
        }
    }
}
