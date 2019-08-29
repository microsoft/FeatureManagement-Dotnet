// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to evaluate whether a feature is enabled or disabled.
    /// </summary>
    class FeatureManager : IFeatureManager
    {
        private readonly IFeatureSettingsProvider _settingsProvider;
        private readonly IEnumerable<IFeatureFilter> _featureFilters;
        private readonly IEnumerable<ISessionManager> _sessionManagers;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, IFeatureFilter> _filterCache;

        public FeatureManager(IFeatureSettingsProvider settingsProvider, IEnumerable<IFeatureFilter> featureFilters, IEnumerable<ISessionManager> sessionManagers, ILoggerFactory loggerFactory)
        {
            _settingsProvider = settingsProvider;
            _featureFilters = featureFilters ?? throw new ArgumentNullException(nameof(featureFilters));
            _sessionManagers = sessionManagers ?? throw new ArgumentNullException(nameof(sessionManagers));
            _logger = loggerFactory.CreateLogger<FeatureManager>();
            _filterCache = new ConcurrentDictionary<string, IFeatureFilter>();
        }

        public async Task<bool> IsEnabledAsync(string feature)
        {
            foreach (ISessionManager sessionManager in _sessionManagers)
            {
                if (await sessionManager.TryGetAsync(feature, out bool cachedEnabled).ConfigureAwait(false))
                {
                    return cachedEnabled;
                }
            }

            bool enabled = false;

            IFeatureSettings settings = _settingsProvider.TryGetFeatureSettings(feature);

            if (settings != null)
            {
                //
                // Check if feature is always on
                // If it is, result is true, goto: cache

                if (settings.EnabledFor.Any(featureFilter => string.Equals(featureFilter.Name, "AlwaysOn", StringComparison.OrdinalIgnoreCase)))
                {
                    enabled = true;
                }
                else
                {
                    //
                    // For all enabling filters listed in the feature's state calculate if they return true
                    // If any executed filters return true, return true

                    foreach (IFeatureFilterSettings featureFilterSettings in settings.EnabledFor)
                    {
                        IFeatureFilter filter = GetFeatureFilter(featureFilterSettings.Name);

                        if (filter == null)
                        {
                            _logger.LogWarning($"Feature filter '{featureFilterSettings.Name}' specified for feature '{feature}' was not found.");

                            continue;
                        }

                        var context = new FeatureFilterEvaluationContext()
                        {
                            FeatureName = feature,
                            Parameters = featureFilterSettings.Parameters 
                        };

                        if (await filter.EvaluateAsync(context).ConfigureAwait(false))
                        {
                            enabled = true;

                            break;
                        }
                    }
                }
            }

            foreach (ISessionManager sessionManager in _sessionManagers)
            {
                await sessionManager.SetAsync(feature, enabled).ConfigureAwait(false);
            }

            return enabled;
        }

        private IFeatureFilter GetFeatureFilter(string filterName)
        {
            const string filterSuffix = "filter";

            IFeatureFilter filter = _filterCache.GetOrAdd(
                filterName,
                (_) => {

                    IEnumerable<IFeatureFilter> matchingFilters = _featureFilters.Where(f =>
                    {
                        Type t = f.GetType();

                        string name = ((FilterAliasAttribute)Attribute.GetCustomAttribute(t, typeof(FilterAliasAttribute)))?.Alias;

                        if (name == null)
                        {
                            name = t.Name.EndsWith(filterSuffix, StringComparison.OrdinalIgnoreCase) ? t.Name.Substring(0, t.Name.Length - filterSuffix.Length) : t.Name;
                        }

                        //
                        // Feature filters can have namespaces in their alias
                        // If a feature is configured to use a filter without a namespace such as 'MyFilter', then it can match 'MyOrg.MyProduct.MyFilter' or simply 'MyFilter'
                        // If a feature is configured to use a filter with a namespace such as 'MyOrg.MyProduct.MyFilter' then it can only match 'MyOrg.MyProduct.MyFilter' 
                        if (filterName.Contains('.'))
                        {
                            //
                            // The configured filter name is namespaced. It must be an exact match.
                            return string.Equals(name, filterName, StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            //
                            // We take the simple name of a filter, E.g. 'MyFilter' for 'MyOrg.MyProduct.MyFilter'
                            string simpleName = name.Contains('.') ? name.Split('.').Last() : name;

                            return string.Equals(simpleName, filterName, StringComparison.OrdinalIgnoreCase);
                        }
                    });

                    if (matchingFilters.Count() > 1)
                    {
                        throw new InvalidOperationException($"Multiple feature filters match the configured filter named '{filterName}'.");
                    }

                    return matchingFilters.FirstOrDefault();
                }
            );

            return filter;
        }
    }
}
