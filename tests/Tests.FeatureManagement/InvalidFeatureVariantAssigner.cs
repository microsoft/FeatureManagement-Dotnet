// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    //
    // Cannot implement more than one IFeatureVariantAssigner interface
    class InvalidFeatureVariantAssigner : IContextualFeatureVariantAssigner<IAccountContext>, IContextualFeatureVariantAssigner<object>
    {
        public ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext variantAssignmentContext, IAccountContext appContext, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext variantAssignmentContext, object appContext, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }

    //
    // Cannot implement more than one IFeatureVariantAssigner interface
    class InvalidFeatureVariantAssigner2 : IFeatureVariantAssigner, IContextualFeatureVariantAssigner<object>
    {
        public ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext variantAssignmentContext, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext variantAssignmentContext, object appContext, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
