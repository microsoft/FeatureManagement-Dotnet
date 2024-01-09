// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace Microsoft.FeatureManagement
{
    internal class FeaturedServiceImplementationWrapper<TService> where TService : class
    {
        public string FeatureName { get; init; }

        public TService Implementation { get; init; }
    }
}
