// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.FeatureManagement
{
    internal class VariantConfigurationSection : IConfigurationSection
    {
        private readonly string _key;
        private readonly string _path;

        public VariantConfigurationSection(string key, string path, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            _path = path;
            _key = key;
            Value = value;
        }

        public string this[string key]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string Key => _key;

        public string Path => _path;

        public string Value { get; set; }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return Enumerable.Empty<IConfigurationSection>();
        }

        public IChangeToken GetReloadToken()
        {
            return new CancellationChangeToken(CancellationToken.None);
        }

        public IConfigurationSection GetSection(string key)
        {
            return new VariantConfigurationSection(key, key, null);
        }
    }
}
