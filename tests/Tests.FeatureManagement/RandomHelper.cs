// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Text;

namespace Tests.FeatureManagement
{
    class RandomHelper
    {
        private static Random s_random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";

        public static string GetRandomString(int length)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[s_random.Next(chars.Length) % chars.Length]);
            }

            return sb.ToString();
        }
    }
}
