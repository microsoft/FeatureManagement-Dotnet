// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to evaluate whether a feature is enabled or disabled.
    /// </summary>
    public interface IFeatureManager
    {
        /// <summary>
        /// Checks whether a given feature is enabled.
        /// </summary>
        /// <param name="feature">The name of the feature to check.</param>
        /// <returns>True if the feature is enabled, otherwise false.</returns>
        Task<bool> IsEnabledAsync(string feature);
    }
}
