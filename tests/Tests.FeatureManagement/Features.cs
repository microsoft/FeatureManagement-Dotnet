// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Tests.FeatureManagement
{
    static class Features
    {
        public const string TargetingTestFeature = "TargetingTestFeature";
        public const string TargetingTestFeatureWithExclusion = "TargetingTestFeatureWithExclusion";
        public const string OnTestFeature = "OnTestFeature";
        public const string OffTestFeature = "OffTestFeature";
        public const string AlwaysOnTestFeature = "AlwaysOnTestFeature";
        public const string OffTimeTestFeature = "OffTimeTestFeature";
        public const string ConditionalFeature = "ConditionalFeature";
        public const string ConditionalFeature2 = "ConditionalFeature2";
        public const string ContextualFeature = "ContextualFeature";
        public const string AnyFilterFeature = "AnyFilterFeature";
        public const string AllFilterFeature = "AllFilterFeature";
        public const string FeatureUsesFiltersWithDuplicatedAlias = "FeatureUsesFiltersWithDuplicatedAlias";
        public const string VariantFeatureDefaultEnabled = "VariantFeatureDefaultEnabled";
        public const string VariantFeatureStatusDisabled = "VariantFeatureStatusDisabled";
        public const string VariantFeaturePercentileOn = "VariantFeaturePercentileOn";
        public const string VariantFeaturePercentileOff = "VariantFeaturePercentileOff";
        public const string VariantFeatureAlwaysOff = "VariantFeatureAlwaysOff";
        public const string VariantFeatureUser = "VariantFeatureUser";
        public const string VariantFeatureGroup = "VariantFeatureGroup";
        public const string VariantFeatureNoVariants = "VariantFeatureNoVariants";
        public const string VariantFeatureNoAllocation = "VariantFeatureNoAllocation";
        public const string VariantFeatureAlwaysOffNoAllocation = "VariantFeatureAlwaysOffNoAllocation";
        public const string VariantFeatureBothConfigurations = "VariantFeatureBothConfigurations";
        public const string VariantFeatureInvalidStatusOverride = "VariantFeatureInvalidStatusOverride";
        public const string VariantFeatureInvalidFromTo = "VariantFeatureInvalidFromTo";
        public const string VariantImplementationFeature = "VariantImplementationFeature";
        public const string VariantTestFeature = "VariantTestFeature";
    }
}
