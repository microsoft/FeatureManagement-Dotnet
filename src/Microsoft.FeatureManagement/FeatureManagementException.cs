// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Represents errors that occur during feature management.
    /// </summary>
    public class FeatureManagementException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureManagementException"/> class.
        /// </summary>
        /// <param name="errorType">The feature management error that the exception represents.</param>
        /// <param name="message">Error message for the exception.</param>
        public FeatureManagementException(FeatureManagementError errorType, string message)
            : base(message)
        {
            Error = errorType;
        }

        /// <summary>
        /// The feature management error that the exception represents.
        /// </summary>
        public FeatureManagementError Error { get; set; }
    }
}
