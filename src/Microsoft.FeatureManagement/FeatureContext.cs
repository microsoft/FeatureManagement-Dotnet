namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Context used to evaluate feature flags.
    /// </summary>
    public class FeatureContext : IFeatureContext
    {
        /// <summary>
        /// Context ID.
        /// Used to determine uniqueness of a context.
        /// Generated and provided by the caller. 
        /// </summary>
        public string ID { get; private set; }

        /// <summary>
        /// Creates feature context.
        /// </summary>
        /// <param name="id">
        /// Feature context identifier.
        /// Optional if contexts are not required to be unique.
        /// </param>
        public FeatureContext(string id = null)
        {
            ID = id;
        }
    }
}