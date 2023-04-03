﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly IFeatureDefinitionProvider _featureDefinitionProvider;
        private readonly IEnumerable<IFeatureFilterMetadata> _featureFilters;
        private readonly IEnumerable<ISessionManager> _sessionManagers;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, IFeatureFilterMetadata> _filterMetadataCache;
        private readonly ConcurrentDictionary<string, ContextualFeatureFilterEvaluator> _contextualFeatureFilterCache;
        private readonly FeatureManagementOptions _options;

        public FeatureManager(
            IFeatureDefinitionProvider featureDefinitionProvider,
            IEnumerable<IFeatureFilterMetadata> featureFilters,
            IEnumerable<ISessionManager> sessionManagers,
            ILoggerFactory loggerFactory,
            IOptions<FeatureManagementOptions> options)
        {
            _featureDefinitionProvider = featureDefinitionProvider;
            _featureFilters = featureFilters ?? throw new ArgumentNullException(nameof(featureFilters));
            _sessionManagers = sessionManagers ?? throw new ArgumentNullException(nameof(sessionManagers));
            _logger = loggerFactory.CreateLogger<FeatureManager>();
            _filterMetadataCache = new ConcurrentDictionary<string, IFeatureFilterMetadata>();
            _contextualFeatureFilterCache = new ConcurrentDictionary<string, ContextualFeatureFilterEvaluator>();
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public Task<bool> IsEnabledAsync(string feature)
        {
            return IsEnabledAsync<object>(feature, null, false);
        }

        public Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext)
        {
            return IsEnabledAsync(feature, appContext, true);
        }

        public async IAsyncEnumerable<string> GetFeatureNamesAsync()
        {
            await foreach (FeatureDefinition featureDefintion in _featureDefinitionProvider.GetAllFeatureDefinitionsAsync().ConfigureAwait(false))
            {
                yield return featureDefintion.Name;
            }
        }

        private async Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext, bool useAppContext)
        {
            foreach (ISessionManager sessionManager in _sessionManagers)
            {
                bool? readSessionResult = await sessionManager.GetAsync(feature).ConfigureAwait(false);

                if (readSessionResult.HasValue)
                {
                    return readSessionResult.Value;
                }
            }

            bool enabled = false;

            FeatureDefinition featureDefinition = await _featureDefinitionProvider.GetFeatureDefinitionAsync(feature).ConfigureAwait(false);

            if (featureDefinition != null)
            {
                //
                // If the requirement type is Any, we end on a true. Requirement type All will end on a false
                bool targetEvaluation = featureDefinition.RequirementType == RequirementType.Any;

                //
                // For all enabling filters listed in the feature's state, evaluate them according to requirement type
                foreach (FeatureFilterConfiguration featureFilterConfiguration in featureDefinition.EnabledFor)
                {
                    //
                    // If the filter is AlwaysOn and we're targeting true, return true
                    if (string.Equals(featureFilterConfiguration.Name, "AlwaysOn", StringComparison.OrdinalIgnoreCase) && targetEvaluation)
                    {
                        enabled = true;

                        break;
                    }

                    IFeatureFilterMetadata filter = GetFeatureFilterMetadata(featureFilterConfiguration.Name);

                    if (filter == null)
                    {
                        string errorMessage = $"The feature filter '{featureFilterConfiguration.Name}' specified for feature '{feature}' was not found.";

                        if (!_options.IgnoreMissingFeatureFilters)
                        {
                            throw new FeatureManagementException(FeatureManagementError.MissingFeatureFilter, errorMessage);
                        }
                        else
                        {
                            if (featureDefinition.RequirementType == RequirementType.All)
                            {
                                errorMessage = $"The 'IgnoreMissingFeatureFilters' flag cannot override a missing filter on a feature with requirement type 'All'. {errorMessage}";
                                throw new FeatureManagementException(FeatureManagementError.MissingFeatureFilter, errorMessage);
                            } else
                            {
                                _logger.LogWarning(errorMessage);
                            }
                        }

                        continue;
                    }

                    var context = new FeatureFilterEvaluationContext()
                    {
                        FeatureName = feature,
                        Parameters = featureFilterConfiguration.Parameters
                    };

                    //
                    // Default the result of this filter evaluation to false
                    bool evaluation = false;

                    //
                    // IContextualFeatureFilter
                    if (useAppContext)
                    {
                        ContextualFeatureFilterEvaluator contextualFilter = GetContextualFeatureFilter(featureFilterConfiguration.Name, typeof(TContext));

                        if (contextualFilter != null) {
                            evaluation = await contextualFilter.EvaluateAsync(context, appContext).ConfigureAwait(false) == targetEvaluation;
                        }
                    }

                    if (!evaluation)
                    {
                        //
                        // IFeatureFilter
                        if (filter is IFeatureFilter featureFilter)
                        {
                            evaluation = await featureFilter.EvaluateAsync(context).ConfigureAwait(false);
                        }
                    }

                    //
                    // Set enabled to true if this filter evaluated to true
                    if (evaluation)
                    {
                        enabled = evaluation;
                    }

                    //
                    // Check if the loop should stop
                    if (evaluation == targetEvaluation)
                    {
                        enabled = targetEvaluation;

                        break;
                    }
                }
            }
            else
            {
                string errorMessage = $"The feature declaration for the feature '{feature}' was not found.";

                if (!_options.IgnoreMissingFeatures)
                {
                    throw new FeatureManagementException(FeatureManagementError.MissingFeature, errorMessage);
                }
                else
                {
                    _logger.LogWarning(errorMessage);
                }
            }

            foreach (ISessionManager sessionManager in _sessionManagers)
            {
                await sessionManager.SetAsync(feature, enabled).ConfigureAwait(false);
            }

            return enabled;
        }

        private IFeatureFilterMetadata GetFeatureFilterMetadata(string filterName)
        {
            const string filterSuffix = "filter";

            IFeatureFilterMetadata filter = _filterMetadataCache.GetOrAdd(
                filterName,
                (_) => {

                    IEnumerable<IFeatureFilterMetadata> matchingFilters = _featureFilters.Where(f =>
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
                        throw new FeatureManagementException(FeatureManagementError.AmbiguousFeatureFilter, $"Multiple feature filters match the configured filter named '{filterName}'.");
                    }

                    return matchingFilters.FirstOrDefault();
                }
            );

            return filter;
        }

        private ContextualFeatureFilterEvaluator GetContextualFeatureFilter(string filterName, Type appContextType)
        {
            if (appContextType == null)
            {
                throw new ArgumentNullException(nameof(appContextType));
            }

            ContextualFeatureFilterEvaluator filter = _contextualFeatureFilterCache.GetOrAdd(
                $"{filterName}{Environment.NewLine}{appContextType.FullName}",
                (_) => {

                    IFeatureFilterMetadata metadata = GetFeatureFilterMetadata(filterName);

                    return ContextualFeatureFilterEvaluator.IsContextualFilter(metadata, appContextType) ?
                        new ContextualFeatureFilterEvaluator(metadata, appContextType) :
                        null;
                }
            );

            return filter;
        }
    }
}
