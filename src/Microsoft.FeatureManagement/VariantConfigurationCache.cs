// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.FeatureManagement
{
    internal sealed class VariantConfigurationCache
    {
        private readonly ConcurrentDictionary<Type, Lazy<object>> _configurations =
            new ConcurrentDictionary<Type, Lazy<object>>();

        public VariantConfigurationCache(IConfigurationSection configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IConfigurationSection Configuration { get; }

        public T GetConfiguration<T>()
        {
            Type configurationType = typeof(T);

            if (_configurations.TryGetValue(configurationType, out Lazy<object> configuration))
            {
                return GetValue<T>(configurationType, configuration);
            }

            return GetOrAdd(() => Configuration.Get<T>());
        }

        internal T GetOrAdd<T>(Func<T> valueFactory)
        {
            if (valueFactory == null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            Type configurationType = typeof(T);

            if (!_configurations.TryGetValue(configurationType, out Lazy<object> configuration))
            {
                var newConfiguration = new Lazy<object>(
                    () => valueFactory(),
                    LazyThreadSafetyMode.ExecutionAndPublication);

                configuration = _configurations.GetOrAdd(configurationType, newConfiguration);
            }

            return GetValue<T>(configurationType, configuration);
        }

        private T GetValue<T>(Type configurationType, Lazy<object> configuration)
        {
            try
            {
                return (T)configuration.Value;
            }
            catch
            {
                // A failed binding should not permanently poison this configuration type.
                ((ICollection<KeyValuePair<Type, Lazy<object>>>)_configurations).Remove(
                    new KeyValuePair<Type, Lazy<object>>(configurationType, configuration));

                throw;
            }
        }
    }
}
