// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Consoto.Banking.AccountServer.FeatureFilters;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using System;
using System.Collections.Generic;

namespace Consoto.Banking.AccountServer.FeatureManagement
{
    /// <summary>
    /// A filter that uses the feature management context to ensure that the current task has the notion of an account id, and that the account id is allowed.
    /// </summary>
    [FilterAlias("AccountId")]
    class AccountIdFilter : IContextualFeatureFilter<IAccountContext>
    {
        public bool Evaluate(FeatureFilterEvaluationContext featureEvaluationContext, IAccountContext accountId)
        {
            if (string.IsNullOrEmpty(accountId?.AccountId))
            {
                throw new ArgumentNullException(nameof(accountId));
            }

            var allowedAccounts = new List<string>();

            featureEvaluationContext.Parameters.Bind("AllowedAccounts", allowedAccounts);

            return allowedAccounts.Contains(accountId.AccountId);
        }

        public bool Evaluate(FeatureFilterEvaluationContext context) => false; // No app-context available
    }
}
