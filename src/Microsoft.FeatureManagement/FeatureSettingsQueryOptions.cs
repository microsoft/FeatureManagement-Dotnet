namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Options used to select a set of feature settings.
    /// </summary>
    class FeatureSettingsQueryOptions
    {
        /// <summary>
        /// The name of an individual feature to select.
        /// </summary>
        public string FeatureName { get; set; }

        /// <summary>
        /// A filter used to select all feature settings after the feature with the specified name.
        /// </summary>
        public string After { get; set; }
    }
}
