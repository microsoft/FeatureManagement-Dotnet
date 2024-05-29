
/* Unmerged change from project 'Tests.FeatureManagement(net6.0)'
Before:
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
After:
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.
*/

/* Unmerged change from project 'Tests.FeatureManagement(net7.0)'
Before:
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
After:
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.
*/

/* Unmerged change from project 'Tests.FeatureManagement(net8.0)'
Before:
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
After:
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.
*/
// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

using System.Threading.Tasks;
using Microsoft.FeatureManagement;

namespace Tests.FeatureManagement
{
    interface IDummyContext
    {
        string DummyProperty { get; set; }
    }

    class DummyContext : IDummyContext
    {
        public string DummyProperty { get; set; }
    }

    [FilterAlias(Alias)]
    class DuplicatedAliasFeatureFilter1 : IFeatureFilter
    {
        private const string Alias = "DuplicatedFilterName";

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            return Task.FromResult(true);
        }
    }

    [FilterAlias(Alias)]
    class DuplicatedAliasFeatureFilter2 : IFeatureFilter
    {
        private const string Alias = "DuplicatedFilterName";

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            return Task.FromResult(true);
        }
    }

    [FilterAlias(Alias)]
    class ContextualDuplicatedAliasFeatureFilterWithAccountContext : IContextualFeatureFilter<IAccountContext>
    {
        private const string Alias = "DuplicatedFilterName";

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, IAccountContext accountContext)
        {
            return Task.FromResult(true);
        }
    }

    [FilterAlias(Alias)]
    class ContextualDuplicatedAliasFeatureFilterWithDummyContext1 : IContextualFeatureFilter<IDummyContext>
    {
        private const string Alias = "DuplicatedFilterName";

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, IDummyContext dummyContext)
        {
            return Task.FromResult(true);
        }
    }

    [FilterAlias(Alias)]
    class ContextualDuplicatedAliasFeatureFilterWithDummyContext2 : IContextualFeatureFilter<IDummyContext>
    {
        private const string Alias = "DuplicatedFilterName";

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, IDummyContext dummyContext)
        {
            return Task.FromResult(true);
        }
    }
}
