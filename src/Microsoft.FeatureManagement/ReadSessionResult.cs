namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// The result of reading the feature state for a session.
    /// </summary>
    public struct ReadSessionResult
    {
        /// <summary>
        /// Indicates whether the session manager was able to provide a state for the feature.
        /// </summary>
        public bool HasValue { get; set; }

        /// <summary>
        /// The state from the session, if any.
        /// </summary>
        public bool Value { get; set; }
    }
}
