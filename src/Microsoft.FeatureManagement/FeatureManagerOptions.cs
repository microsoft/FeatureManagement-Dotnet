// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Options that are being used when a feature is being evaluated by the Feature Manager.
    /// </summary>
    public class FeatureManagerOptions
    {
        /// <summary>
        /// Is being used to decide if an exception should be thrown or not when a configured feature filter has not been registered.
        /// </summary>
        public bool SwallowExceptionForUnregisteredFilter { get; set; }
    }
}