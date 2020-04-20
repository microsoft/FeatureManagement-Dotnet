using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.FeatureManagement
{
    /// <summary>
    /// Test Implementation of a service responsible for retrieving feature settings from a third party.
    /// </summary>
    public class TestThirdPartyFeatureSettingsService
    {
        public Task<bool> IsEnabledAsync(string feature)
        {
            var isEnabled = false;
            Enum.TryParse(feature, out Features featureEnum);

            switch (featureEnum)
            {
                case Features.OnTestFeature:
                    isEnabled = true;
                    break;

                case Features.OffTestFeature:
                    break;
            }

            return Task.FromResult(isEnabled);
        }

        public Task<IEnumerable<string>> GetAllFeaturesAsync()
        {
            var features = Enum.GetValues(typeof(Features)).Cast<Features>().Select(x => x.ToString());
            return Task.FromResult(features);
        }
    }
}
