﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Threading;

namespace Microsoft.FeatureManagement.Utils
{
    internal static class RandomGenerator
    {
        private static readonly Random _global = new Random();

        private static readonly ThreadLocal<Random> _rnd = new ThreadLocal<Random>(() =>
        {
            int seed;

            lock (_global)
            {
                seed = _global.Next();
            }

            return new Random(seed);
        });

        public static int Next()
        {
            return _rnd.Value.Next();
        }

        public static double NextDouble()
        {
            return _rnd.Value.NextDouble();
        }
    }
}
