// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement.FeatureFilters
{
    /// <summary>
    /// Options that apply to all uses of a target filter inside a service collection.
    /// </summary>
    public class TargetingEvaluationOptions
    {
        /// <summary>
        /// Used to ignore case when comparing user id and group names during targeting evaluation.
        /// </summary>
        public bool IgnoreCase { get; set; }
    }
}
