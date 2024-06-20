// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;

namespace VariantServiceDemo
{
    [VariantServiceAlias("DefaultCalculator")]
    public class DefaultCalculator : ICalculator
    {
        public Task<double> AddAsync(double a, double b)
        {
            return Task.FromResult(a + b);
        }
    }
}
