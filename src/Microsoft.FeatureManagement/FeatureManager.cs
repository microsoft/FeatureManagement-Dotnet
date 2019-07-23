// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to evaluate whether a feature is enabled or disabled.
    /// </summary>
    class FeatureManager : IFeatureManager, IContextualFeatureManager
    {
        private readonly IFeatureSettingsProvider _settingsProvider;
        private readonly IEnumerable<IFeatureFilter> _featureFilters;
        private readonly IEnumerable<ISessionManager> _sessionManagers;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, IFeatureFilter> _filterCache;
        private readonly ConcurrentDictionary<string, ContextualFeatureFilterEvaluator> _contextualFilterCache;

        public FeatureManager(
            IFeatureSettingsProvider settingsProvider,
            IEnumerable<IFeatureFilter> featureFilters,
            IEnumerable<ISessionManager> sessionManagers,
            ILoggerFactory loggerFactory)
        {
            _settingsProvider = settingsProvider;
            _featureFilters = featureFilters ?? throw new ArgumentNullException(nameof(featureFilters));
            _sessionManagers = sessionManagers ?? throw new ArgumentNullException(nameof(sessionManagers));
            _logger = loggerFactory.CreateLogger<FeatureManager>();
            _filterCache = new ConcurrentDictionary<string, IFeatureFilter>();
            _contextualFilterCache = new ConcurrentDictionary<string, ContextualFeatureFilterEvaluator>();
        }

        public bool IsEnabled(string feature)
        {
            return IsEnabled<IFeatureFilterContext>(feature, null, false);
        }

        public bool IsEnabled<TContext>(string feature, TContext appContext) where TContext : IFeatureFilterContext
        {
            return IsEnabled(feature, appContext, true);
        }

        private bool IsEnabled<TContext>(string feature, TContext appContext, bool useContext) where TContext : IFeatureFilterContext
        {
            foreach (ISessionManager sessionManager in _sessionManagers)
            {
                if (sessionManager.TryGet(feature, out bool cachedEnabled))
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
                        IFeatureFilter filter = GetFeatureFilter(featureFilterSettings.Name, !useContext ? null : typeof(TContext));

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

                        if (filter is ContextualFeatureFilterEvaluator contextualFilterEvaluator && contextualFilterEvaluator.Evaluate(context, appContext))
                        {
                            enabled = true;

                            break;
                        }

                        if (filter != null && filter.Evaluate(context))
                        {
                            enabled = true;

                            break;
                        }
                    }
                }
            }

            foreach (ISessionManager sessionManager in _sessionManagers)
            {
                sessionManager.Set(feature, enabled);
            }

            return enabled;
        }

        private IFeatureFilter GetFeatureFilter(string filterName, Type appContextType = null)
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

            if (filter is IContextualFeatureFilter contextualFeatureFilter && appContextType != null)
            {
                ContextualFeatureFilterEvaluator contextualFeatureFilterEvaluator = _contextualFilterCache.GetOrAdd(
                    filterName + "\n" + appContextType.FullName,
                    (_) => contextualFeatureFilter == null ? null : new ContextualFeatureFilterEvaluator(contextualFeatureFilter, appContextType));
            }

            return filter;
        }
    }
}
