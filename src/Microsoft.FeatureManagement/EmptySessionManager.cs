// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Empty implementation of <see cref="ISessionManager"/>.
    /// </summary>
    class EmptySessionManager : ISessionManager
    {
        public void Set(string featureName, bool enabled)
        {
        }

        public bool TryGet(string featureName, out bool enabled)
        {
            enabled = false;

            return false;
        }
    }
}
