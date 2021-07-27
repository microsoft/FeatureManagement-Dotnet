// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    class TestAssigner : IFeatureVariantAssigner
    {
        public Func<FeatureVariantAssignmentContext, FeatureVariant> Callback { get; set; }

        public ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext variantAssignmentContext, CancellationToken cancellationToken)
        {
            return new ValueTask<FeatureVariant>(Callback(variantAssignmentContext));
        }
    }
}
