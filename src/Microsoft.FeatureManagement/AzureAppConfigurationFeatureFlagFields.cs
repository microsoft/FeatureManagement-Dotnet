// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

namespace Microsoft.FeatureManagement
{
    //
    // Azure App Configuration feature flag schema: https://github.com/Azure/AppConfiguration/blob/main/docs/FeatureManagement/FeatureFlag.v1.1.0.schema.json
    internal static class AzureAppConfigurationFeatureFlagFields
    {
        public const string FeatureFlagsSectionName = "FeatureFlags";

        //
        // Feature flag keywords
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