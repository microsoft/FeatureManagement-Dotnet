﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

namespace Microsoft.FeatureManagement
{
    //
    // Microsoft Feature Management schema: https://github.com/Azure/AppConfiguration/blob/main/docs/FeatureManagement/FeatureManagement.v1.0.0.schema.json
    internal static class MicrosoftFeatureManagementFields
    {
        public const string FeatureManagementSectionName = "feature_management";
        public const string FeatureFlagsSectionName = "feature_flags";

        //
        // Microsoft feature flag keywords
        public const string Id = "id";
        public const string Enabled = "enabled";
        public const string Conditions = "conditions";
        public const string ClientFilters = "client_filters";
        public const string RequirementType = "requirement_type";

        //
        // Client filter keywords
        public const string Name = "name";
        public const string Parameters = "parameters";
    }
}