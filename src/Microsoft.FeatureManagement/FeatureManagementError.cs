namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// An error that can occur during feature management.
    /// </summary>
    public enum FeatureManagementError
    {
        /// <summary>
        /// A feature filter that was listed for feature evaluation was not found.
        /// </summary>
        MissingFeatureFilter,

        /// <summary>
        /// Multiple feature filters match the name listed for feature evaluation.
        /// </summary>
        ConflictingFeatureFilter
    }
}
