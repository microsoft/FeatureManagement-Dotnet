// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Describes whether any or all conditions in a set should be required to be true.
    /// </summary>
    public enum RequirementType
    {
        /// <summary>
        /// The set of conditions will be evaluated as true if any condition in the set is true.
        /// </summary>
        Any,
        /// <summary>
        /// The set of conditions will be evaluated as true if all the conditions in the set are true.
        /// </summary>
        All
    }
}
