// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Marker interface for feature filters used to handle an impression event
    /// </summary>
    public interface IFeatureImpressionsListener
    {
        public bool HandleImpression(string feature, bool treatment);
    }
}
