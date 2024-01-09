// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    internal class FeaturedServiceImplementationWrapper<TService>
    {
        public TService Implementation { get; init; }

        public string FeatureName { get; init; }

        public string VariantName { get; init; }
    }
}
