// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    class ContextualTestAssigner : IContextualFeatureVariantAssigner<IAccountContext>
    {
        public Func<FeatureVariantAssignmentContext, IAccountContext, FeatureVariant> Callback { get; set; }

        public ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext variantAssignmentContext, IAccountContext appContext, CancellationToken cancellationToken)
        {
            return new ValueTask<FeatureVariant>(Callback(variantAssignmentContext, appContext));
        }
    }
}
