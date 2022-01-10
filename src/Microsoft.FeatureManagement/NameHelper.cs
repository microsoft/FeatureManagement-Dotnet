using System;
using System.Linq;

namespace Microsoft.FeatureManagement
{
    static class NameHelper
    {
        /// <summary>
        /// Evaluates whether a feature filter or feature variant assigner reference matches a given feature filter/assigner name.
        /// </summary>
        /// <param name="reference">A reference to some feature metadata that should be checked for a match with the provided metadata name</param>
        /// <param name="metadataName">The name used by the feature filter/feature variant assigner</param>
        /// <param name="suffix">An optional suffix that may be included when referencing the metadata type. E.g. "filter" or "assigner".</param>
        /// <returns>True if the reference is a match for the metadata name. False otherwise.</returns>
        public static bool IsMatchingReference(string reference, string metadataName, string suffix)
        {
            if (string.IsNullOrEmpty(reference))
            {
                throw new ArgumentNullException(nameof(reference));
            }

            if (string.IsNullOrEmpty(metadataName))
            {
                throw new ArgumentNullException(nameof(metadataName));
            }

            //
            // Feature filters/assigner can be referenced with or without their associated suffix ('filter' or 'assigner')
            // E.g. A feature can reference a filter named 'CustomFilter' as 'Custom' or 'CustomFilter'
            if (!reference.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
                metadataName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                metadataName = metadataName.Substring(0, metadataName.Length - suffix.Length);
            }

            //
            // Feature filters/assigners can have namespaces in their alias
            // If a feature is configured to use a filter without a namespace such as 'MyFilter', then it can match 'MyOrg.MyProduct.MyFilter' or simply 'MyFilter'
            // If a feature is configured to use a filter with a namespace such as 'MyOrg.MyProduct.MyFilter' then it can only match 'MyOrg.MyProduct.MyFilter' 
            if (reference.Contains('.'))
            {
                //
                // The configured metadata name is namespaced. It must be an exact match.
                return string.Equals(metadataName, reference, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                //
                // We take the simple name of the metadata, E.g. 'MyFilter' for a feature filter named 'MyOrg.MyProduct.MyFilter'
                string simpleName = metadataName.Contains('.') ? metadataName.Split('.').Last() : metadataName;

                return string.Equals(simpleName, reference, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
