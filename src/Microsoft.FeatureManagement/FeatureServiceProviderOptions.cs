namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Specifies the keys used by a feature service provider to resolve an implementation based on the feature flag status when keyed di is available.
    /// </summary>
    public class FeatureServiceProviderOptions
    {
        /// <summary>
        /// The key used to resolve the service when the feature flag is enabled and keyed di is available.
        /// </summary>
        public object EnabledKey { get; set; } = true;

        /// <summary>
        /// The alias used to resolve the service when the feature flag is disabled and keyed di is available.
        /// </summary>
        public object DisabledKey { get; set; } = false;
    }
}
