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
        /// Controls whether to enable the custom configuration merging logic for Microsoft schema feature flags or fall back to .NET's native configuration merging behavior.
        /// </summary>
        /// <remarks>
        /// This option only affects Microsoft schema feature flags (e.g. feature_management:feature_flags arrays). .NET schema feature flags are not affected by this setting.
        /// 
        /// The <see cref="ConfigurationFeatureDefinitionProvider"/> uses custom configuration merging logic for Microsoft schema feature flags to ensure that
        /// feature flags with the same ID from different configuration sources are merged correctly based on their logical identity rather than array position.
        /// By default, the provider bypasses .NET's native array merging behavior which merges arrays by index position and can lead to unexpected results when feature flags are defined across multiple configuration sources.
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
