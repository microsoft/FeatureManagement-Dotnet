using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Microsoft.FeatureManagement
{
    internal class OnDemandConfigurationProvider : ConfigurationProvider
    {
        public OnDemandConfigurationProvider(IDictionary<string, string> data)
        {
            Data = data;
        }
    }
}
