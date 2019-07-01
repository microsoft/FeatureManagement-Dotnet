// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A provider of feature settings.
    /// </summary>
    interface IFeatureSettingsProvider
    {
        /// <summary>
        /// Retrieves settings for a given feature.
        /// </summary>
        /// <param name="featureName">The name of the feature to retrieve settings for.</param>
        /// <returns>The feature's settings.</returns>
        IFeatureSettings TryGetFeatureSettings(string featureName);
    }
}
