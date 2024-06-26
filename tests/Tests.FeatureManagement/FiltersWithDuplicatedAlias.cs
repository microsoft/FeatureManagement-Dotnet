// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    internal interface IDummyContext
    {
        string DummyProperty { get; set; }
    }

    internal class DummyContext : IDummyContext
    {
        public string DummyProperty { get; set; }
    }

    [FilterAlias(Alias)]
    internal class DuplicatedAliasFeatureFilter1 : IFeatureFilter
    {
        private const string Alias = "DuplicatedFilterName";

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            return Task.FromResult(true);
        }
    }

    [FilterAlias(Alias)]
    internal class DuplicatedAliasFeatureFilter2 : IFeatureFilter
    {
        private const string Alias = "DuplicatedFilterName";

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            return Task.FromResult(true);
        }
    }

    [FilterAlias(Alias)]
    internal class ContextualDuplicatedAliasFeatureFilterWithAccountContext : IContextualFeatureFilter<IAccountContext>
    {
        private const string Alias = "DuplicatedFilterName";

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, IAccountContext accountContext)
        {
            return Task.FromResult(true);
        }
    }

    [FilterAlias(Alias)]
    internal class ContextualDuplicatedAliasFeatureFilterWithDummyContext1 : IContextualFeatureFilter<IDummyContext>
    {
        private const string Alias = "DuplicatedFilterName";

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, IDummyContext dummyContext)
        {
            return Task.FromResult(true);
        }
    }

    [FilterAlias(Alias)]
    internal class ContextualDuplicatedAliasFeatureFilterWithDummyContext2 : IContextualFeatureFilter<IDummyContext>
    {
        private const string Alias = "DuplicatedFilterName";

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context, IDummyContext dummyContext)
        {
            return Task.FromResult(true);
        }
    }
}
