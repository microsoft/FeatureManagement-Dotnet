namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Common interface for all feature contexts.
    /// </summary>
    public interface IFeatureContext
    {
        /// <summary>
        /// Context ID.
        /// Used to determine uniqueness of a context.
        /// Generated and provided by the caller. 
        /// </summary>
        string ID { get; }
    }
}