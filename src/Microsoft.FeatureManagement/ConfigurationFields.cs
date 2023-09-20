// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

namespace Microsoft.FeatureManagement
{
    internal static class ConfigurationFields
    {
        // Enum keywords
        public const string RequirementType = "RequirementType";
        public const string FeatureStatus = "Status";

        // Feature filters keywords
        public const string FeatureFiltersSectionName = "EnabledFor";
        public const string FeatureFilterConfigurationParameters = "Parameters";

        // Allocation keywords
        public const string AllocationSectionName = "Allocation";
        public const string AllocationDefaultWhenDisabled = "DefaultWhenDisabled";
        public const string AllocationDefaultWhenEnabled = "DefaultWhenEnabled";
        public const string UserAllocationSectionName = "User";
        public const string AllocationVariantKeyword = "Variant";
        public const string UserAllocationUsers = "Users";
        public const string GroupAllocationSectionName = "Group";
        public const string GroupAllocationGroups = "Groups";
        public const string PercentileAllocationSectionName = "Percentile";
        public const string PercentileAllocationFrom = "From";
        public const string PercentileAllocationTo = "To";
        public const string AllocationSeed = "Seed";

        // Variants keywords
        public const string VariantsSectionName = "Variants";
        public const string VariantDefinitionConfigurationValue = "ConfigurationValue";
        public const string VariantDefinitionConfigurationReference = "ConfigurationReference";
        public const string VariantDefinitionStatusOverride = "StatusOverride";

        // Other keywords
        public const string NameKeyword = "Name";
        public const string FeatureManagementSectionName = "FeatureManagement";
    }
}
