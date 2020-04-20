using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.FeatureManagement;

namespace Tests.FeatureManagement
{
    /// <summary>
    /// Test Implementation of IFeatureSettingsProvider.
    /// </summary>
    public class TestFeatureSettingsProvider : IFeatureSettingsProvider
    {
        public TestThirdPartyFeatureSettingsService FeatureSettingsService { get; set; }

        public TestFeatureSettingsProvider()
        {
            FeatureSettingsService = new TestThirdPartyFeatureSettingsService();
        }

        public async IAsyncEnumerable<FeatureSettings> GetAllFeatureSettingsAsync()
        {
            var features = await FeatureSettingsService.GetAllFeaturesAsync();
            foreach (var feature in features.Select(x => new FeatureSettings() { Name = x }))
            {
                yield return feature;
            }
        }

        public async Task<FeatureSettings> GetFeatureSettingsAsync(string featureName)
        {
            if (string.IsNullOrWhiteSpace(featureName))
            {
                throw new ArgumentNullException(nameof(featureName));
            }

            bool fetchedSetting = await FeatureSettingsService.IsEnabledAsync(featureName);
            
            var filterSettings = new List<FeatureFilterSettings>();
            
            var setting = new FeatureSettings()
            {
                Name = featureName,
            };

            if (fetchedSetting)
            {
                filterSettings.Add(new FeatureFilterSettings()
                {
                    Name = "AlwaysOn",
                });

                setting.EnabledFor = filterSettings;
            }

            return setting;
        }
    }
}
