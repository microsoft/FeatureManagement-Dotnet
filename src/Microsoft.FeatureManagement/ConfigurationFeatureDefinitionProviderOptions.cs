// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Options that control the behavior of the <see cref="ConfigurationFeatureDefinitionProvider"/>.
    /// </summary>
    public class ConfigurationFeatureDefinitionProviderOptions
    {
        /// <summary>
        /// Controls whether to enable custom configuration merging for feature flags from multiple configuration sources.
        /// </summary>
        /// <remarks>
        /// The <see cref="ConfigurationFeatureDefinitionProvider"/> uses custom configuration merging logic to ensure that feature flags with the same ID from
        /// different configuration sources are merged correctly based on their logical identity rather than array position. The last configuration source that
        /// defines a feature flag wins, even when earlier and later sources use different feature management schemas. If the same configuration source defines a
        /// feature flag in both the .NET schema and the Microsoft schema, the Microsoft schema definition takes precedence.
        ///
        /// When custom merging is enabled, both .NET and Microsoft schema feature flag definitions are selected as a whole from the winning source.
        /// Properties, feature filters, and filter parameters are not merged across sources. Each overriding source must supply the complete intended definition.
        /// This bypasses .NET's native array merging behavior, which merges arrays by index position and can lead to unexpected results when feature flags are defined across multiple configuration sources.
        /// For configuration that cannot be inspected provider by provider, the combined configuration is treated as a single source.
        /// When custom merging is disabled (the default), .NET's native configuration merging behavior is used, and Microsoft schema definitions take precedence
        /// over .NET schema definitions regardless of configuration source order.
        ///
        /// Consider the following configuration sources:
        /// Configuration Source 1:
        /// {
        ///   "feature_management": {
        ///     "feature_flags": [
        ///       {
        ///         "id": "feature1",
        ///         "enabled": true
        ///       },
        ///       {
        ///         "id": "feature2", 
        ///         "enabled": false
        ///       }
        ///     ]
        ///   }
        /// }
        /// 
        /// Configuration Source 2:
        /// {
        ///   "feature_management": {
        ///     "feature_flags": [
        ///       {
        ///         "id": "feature2",
        ///         "enabled": true
        ///       }
        ///     ]
        ///   }
        /// }
        /// 
        /// With custom merging:
        /// - feature1: enabled = true
        /// - feature2: enabled = true (last declaration wins)
        /// 
        /// With native .NET merging:
        /// - feature1 would be overwritten by feature2 from source 2 (index-based merging, e.g. feature_flags:0:id)
        /// - feature2: enabled = false (from source 1, index 1)
        /// </remarks>
        public bool CustomConfigurationMergingEnabled { get; set; }
    }
}
