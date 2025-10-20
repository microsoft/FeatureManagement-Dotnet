// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
namespace VariantServiceDemo
{
    public interface ICalculator
    {
        public Task<double> AddAsync(double a, double b);
    }
}
