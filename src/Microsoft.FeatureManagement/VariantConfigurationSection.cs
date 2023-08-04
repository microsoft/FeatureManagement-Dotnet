// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.FeatureManagement
{
    internal class VariantConfigurationSection : IConfigurationSection
    {
        private readonly ConfigurationRoot _root;
        private readonly string _path;
        private string _key;

        public VariantConfigurationSection(string key, string path, string value)
        {
            MemoryConfigurationSource source = new MemoryConfigurationSource();

            _root = new ConfigurationRoot(new List<IConfigurationProvider> { new MemoryConfigurationProvider(source) });
            _path = path;
            _key = key;
            Value = value;
        }

        public string this[string key]
        {
            get
            {
                return _root[ConfigurationPath.Combine(Path, key)];
            }
            set
            {
                _root[ConfigurationPath.Combine(Path, key)] = value;
            }
        }

        public string Key => _key;

        public string Path => _path;

        public string Value
        {
            get
            {
                return _root[Path];
            }
            set
            {
                _root[Path] = value;
            }
        }

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
