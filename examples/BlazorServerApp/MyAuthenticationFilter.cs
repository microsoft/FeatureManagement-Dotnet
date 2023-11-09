// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;

namespace BlazorServerApp
{
    [FilterAlias(Alias)]
    public class MyAuthenticationFilter : IFeatureFilter
    {
        private const string Alias = "Auth";
        private readonly HttpContextProvider _contextProvider;

        public MyAuthenticationFilter(HttpContextProvider contextProvider)
        {
            _contextProvider = contextProvider;
        }

        public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
        {
            return Task.FromResult(_contextProvider.IsAuthenticated);
        }
    }
}
