// Copyright (c) Microsoft Corporation.
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
        // Allocation keywords
        public const string AllocationSectionName = "allocation";
        public const string AllocationDefaultWhenDisabled = "default_when_disabled";
        public const string AllocationDefaultWhenEnabled = "default_when_enabled";
        public const string AllocationVariantKeyword = "variant";
        public const string UserAllocationSectionName = "user";
        public const string UserAllocationUsers = "users";
        public const string GroupAllocationSectionName = "group";
        public const string GroupAllocationGroups = "groups";
        public const string PercentileAllocationSectionName = "percentile";
        public const string PercentileAllocationFrom = "from";
        public const string PercentileAllocationTo = "to";
        public const string AllocationSeed = "seed";

        //
        // Client filter keywords
        public const string Name = "name";
        public const string Parameters = "parameters";

        // Variants keywords
        public const string VariantsSectionName = "variants";
        public const string VariantDefinitionConfigurationValue = "configuration_value";
        public const string VariantDefinitionConfigurationReference = "configuration_reference";
        public const string VariantDefinitionStatusOverride = "status_override";

        // Telemetry keywords
        public const string Telemetry = "telemetry";
        public const string Metadata = "metadata";
    }
}