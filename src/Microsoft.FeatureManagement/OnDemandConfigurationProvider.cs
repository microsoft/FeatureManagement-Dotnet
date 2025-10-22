using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.FeatureManagement
{
    internal class OnDemandConfigurationProvider : ConfigurationProvider
    {
        private static readonly PropertyInfo _DataProperty = typeof(ConfigurationProvider).GetProperty(nameof(Data), BindingFlags.NonPublic | BindingFlags.Instance);

        public OnDemandConfigurationProvider(ConfigurationProvider configurationProvider)
        {
            var data = _DataProperty.GetValue(configurationProvider) as IDictionary<string, string>;

            Data = data;
        }
    }
}
