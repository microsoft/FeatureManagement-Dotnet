// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT license.

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a snapshot of feature state to ensure consistency across a given request.
    /// </summary>
    public interface IFeatureManagerSnapshot : IFeatureManager
    {
    }
}
