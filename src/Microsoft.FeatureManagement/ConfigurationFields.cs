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

        // App Config server side schema keywords
        public const string IdKeyword = "id";
        public const string EnabledKeyword = "enabled";
        public const string ConditionsKeyword = "conditions";
        public const string ClientFiltersSectionName = "client_filters";
        public const string LowercaseFeatureManagementSectionName = "name";
        public const string LowercaseFeatureManagementSectionParameters = "parameters";
        public const string LowercaseRequirementType = "requirement_type";
    }
}