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
        public const string ServerSideIdKeyword = "id";
        public const string ServerSideEnabledKeyword = "enabled";
        public const string ServerSideConditionsKeyword = "conditions";
        public const string ServerSideRequirementType = "requirement_type";
        public const string ServerSideFeatureFiltersSectionName = "client_filters";
        public const string ServerSideNameKeyword = "name";
        public const string ServerSideFeatureFilterConfigurationParameters = "parameters";
    }
}