// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;

namespace Microsoft.FeatureManagement
{
    public class VariantAliasAttribute : Attribute
    {
        public VariantAliasAttribute(string alias)
        {
            if (string.IsNullOrEmpty(alias))
            {
                throw new ArgumentNullException(nameof(alias));
            }

            Alias = alias;
        }

        public string Alias { get; }
    }
}
