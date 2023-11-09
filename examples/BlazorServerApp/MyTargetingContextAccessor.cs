// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement.FeatureFilters;

namespace BlazorServerApp
{
    public class MyTargetingContextAccessor : ITargetingContextAccessor
    {
        private readonly HttpContextProvider _contextProvider;

        public MyTargetingContextAccessor(HttpContextProvider contextProvider)
        {
            _contextProvider = contextProvider;
        }

        public ValueTask<TargetingContext> GetContextAsync()
        {
            //
            // Build targeting context based on user info
            string username = _contextProvider.Username;

            var groups = new List<string>();

            if (username != null && username.EndsWith("@vip.com"))
            {
                groups.Add("vip");
            }

            var targetingContext = new TargetingContext
            {
                UserId = username,
                Groups = groups
            };

            return new ValueTask<TargetingContext>(targetingContext);
        }
    }
}
