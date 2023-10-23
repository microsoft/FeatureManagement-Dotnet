// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using System.Threading.Tasks;

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
