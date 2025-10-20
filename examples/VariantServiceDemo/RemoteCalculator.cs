// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.FeatureManagement;

namespace VariantServiceDemo
{
    [VariantServiceAlias("RemoteCalculator")]
    public class RemoteCalculator : ICalculator
    {
        public async Task<double> AddAsync(double a, double b)
        {
            //
            // simulate the latency caused by calling API from a remote server
            await Task.Delay(1000);

            return a + b;
        }
    }
}
