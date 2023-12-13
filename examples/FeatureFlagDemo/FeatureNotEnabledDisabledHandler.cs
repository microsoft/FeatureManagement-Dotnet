// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.FeatureManagement.Mvc;

namespace FeatureFlagDemo.FeatureManagement
{
    public class FeatureNotEnabledDisabledHandler : IDisabledFeaturesHandler
    {
        public Task HandleDisabledFeatures(IEnumerable<string> features, ActionExecutingContext context)
        {
            var result = new ViewResult()
            {
                ViewName = "Views/Shared/FeatureNotEnabled.cshtml",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            };

            result.ViewData["FeatureName"] = string.Join(", ", features);

            context.Result = result;

            return Task.CompletedTask;
        }
    }
}
