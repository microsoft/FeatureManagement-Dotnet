// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    [FilterAlias(Alias)]
    class CustomTargetingFilter : IFeatureFilter
    {
        private const string Alias = "CustomTargetingFilter";
        private readonly ContextualTargetingFilter _contextualFilter;

        public CustomTargetingFilter(IOptions<TargetingEvaluationOptions> options, ILoggerFactory loggerFactory)
        {
            _contextualFilter = new ContextualTargetingFilter(options, loggerFactory);
        }

        public Func<FeatureFilterEvaluationContext, Task<bool>> Callback { get; set; }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            return _contextualFilter.EvaluateAsync(context, new TargetingContext(){ UserId = "Jeff" });
        }
    }
}
