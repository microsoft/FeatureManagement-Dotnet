// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

namespace Microsoft.FeatureManagement
{
    internal static class ConfigurationFields
    {
        // Enum keywords
        public const string RequirementType = "RequirementType";

        // Feature filters keywords
        public const string FeatureFiltersSectionName = "EnabledFor";
        public const string FeatureFilterConfigurationParameters = "Parameters";

        // Other keywords
        public const string NameKeyword = "Name";
        public const string FeatureManagementSectionName = "FeatureManagement";
        public const string FeatureFlagsSectionName = "FeatureFlags";

        // Azure App Configuration feature flag schema keywords
        public const string FeatureFlagId = "id";
        public const string FeatureFlagEnabled = "enabled";
        public const string FeatureFlagConditions = "conditions";
        public const string FeatureFlagClientFilters = "client_filters";
        public const string LowercaseNameKeyword = "name";
        public const string LowercaseFeatureFilterConfigurationParameters = "parameters";
        public const string LowercaseRequirementType = "requirement_type";
    }
}