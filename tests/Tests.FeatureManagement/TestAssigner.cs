// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    class TestAssigner : IFeatureVariantAssigner, IFilterParametersBinder
    {
        public Func<FeatureVariantAssignmentContext, FeatureVariant> Callback { get; set; }

        public Func<IConfiguration, object> ParametersBinderCallback { get; set; }

        public object BindParameters(IConfiguration parameters)
        {
            if (ParametersBinderCallback != null)
            {
                return ParametersBinderCallback(parameters);
            }

            return parameters;
        }

        public ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext variantAssignmentContext, CancellationToken cancellationToken)
        {
            return new ValueTask<FeatureVariant>(Callback(variantAssignmentContext));
        }
    }
}
