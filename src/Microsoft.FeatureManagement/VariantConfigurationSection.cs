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
        private readonly string _path;
        private string _valueForKey;
        private string _key;

        public VariantConfigurationSection(string key, string value)
        {
            _key = key;
            _path = key;
            Value = value;
        }

        public string this[string key] 
        { 
            get
            {
                if (key == _key)
                {
                    return _valueForKey;
                }
                return null;
            }
            set
            {
                if (key == _key)
                {
                    _valueForKey = value;
                }
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
            throw new NotImplementedException();
        }
    }
}
