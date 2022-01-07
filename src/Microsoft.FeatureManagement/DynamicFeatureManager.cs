// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Used to retrieve variants for dynamic features.
    /// </summary>
    class DynamicFeatureManager : IDynamicFeatureManager
    {
        private readonly IDynamicFeatureDefinitionProvider _featureDefinitionProvider;
        private readonly IEnumerable<IFeatureVariantAssignerMetadata> _variantAssigners;
        private readonly IFeatureVariantOptionsResolver _variantOptionsResolver;
        private readonly ConcurrentDictionary<string, IFeatureVariantAssignerMetadata> _assignerMetadataCache;
        private readonly ConcurrentDictionary<string, ContextualFeatureVariantAssignerEvaluator> _contextualFeatureVariantAssignerCache;

        public DynamicFeatureManager(
            IDynamicFeatureDefinitionProvider featureDefinitionProvider,
            IEnumerable<IFeatureVariantAssignerMetadata> variantAssigner,
            IFeatureVariantOptionsResolver variantOptionsResolver)
        {
            _variantAssigners = variantAssigner ?? throw new ArgumentNullException(nameof(variantAssigner));
            _variantOptionsResolver = variantOptionsResolver ?? throw new ArgumentNullException(nameof(variantOptionsResolver));
            _featureDefinitionProvider = featureDefinitionProvider ?? throw new ArgumentNullException(nameof(featureDefinitionProvider));
            _assignerMetadataCache = new ConcurrentDictionary<string, IFeatureVariantAssignerMetadata>(StringComparer.OrdinalIgnoreCase);
            _contextualFeatureVariantAssignerCache = new ConcurrentDictionary<string, ContextualFeatureVariantAssignerEvaluator>(StringComparer.OrdinalIgnoreCase);
        }

        public async IAsyncEnumerable<string> GetDynamicFeatureNamesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (DynamicFeatureDefinition featureDefintion in _featureDefinitionProvider.GetAllDynamicFeatureDefinitionsAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return featureDefintion.Name;
            }
        }

        public ValueTask<T> GetVariantAsync<T, TContext>(string feature, TContext appContext, CancellationToken cancellationToken)
        {
            return GetVariantAsync<T, TContext>(feature, appContext, true, cancellationToken);
        }

        public ValueTask<T> GetVariantAsync<T>(string feature, CancellationToken cancellationToken)
        {
            return GetVariantAsync<T, object>(feature, null, false, cancellationToken);
        }

        private async ValueTask<T> GetVariantAsync<T, TContext>(string feature, TContext appContext, bool useAppContext, CancellationToken cancellationToken)
        {
            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            FeatureVariant variant = null;

            DynamicFeatureDefinition featureDefinition = await _featureDefinitionProvider
                .GetDynamicFeatureDefinitionAsync(feature, cancellationToken)
                .ConfigureAwait(false);

            if (featureDefinition == null)
            {
                throw new FeatureManagementException(
                    FeatureManagementError.MissingFeature,
                    $"The feature declaration for the dynamic feature '{feature}' was not found.");
            }

            if (string.IsNullOrEmpty(featureDefinition.Assigner))
            {
                throw new FeatureManagementException(
                    FeatureManagementError.MissingFeatureVariantAssigner,
                    $"Missing feature variant assigner name for the feature {feature}");
            }

            if (featureDefinition.Variants == null)
            {
                throw new FeatureManagementException(
                    FeatureManagementError.MissingFeatureVariant,
                    $"No variants are registered for the feature {feature}");
            }

            FeatureVariant defaultVariant = null;

            foreach (FeatureVariant v in featureDefinition.Variants)
            {
                if (v.Default)
                {
                    if (defaultVariant != null)
                    {
                        throw new FeatureManagementException(
                            FeatureManagementError.AmbiguousDefaultFeatureVariant,
                            $"Multiple default variants are registered for the feature '{feature}'.");
                    }

                    defaultVariant = v;
                }

                if (v.ConfigurationReference == null)
                {
                    throw new FeatureManagementException(
                        FeatureManagementError.MissingConfigurationReference,
                        $"The variant '{variant.Name}' for the feature '{feature}' does not have a configuration reference.");
                }
            }

            if (defaultVariant == null)
            {
                throw new FeatureManagementException(
                    FeatureManagementError.MissingDefaultFeatureVariant,
                    $"A default variant cannot be found for the feature '{feature}'.");
            }

            IFeatureVariantAssignerMetadata assigner = GetFeatureVariantAssignerMetadata(featureDefinition.Assigner);

            if (assigner == null)
            {
                throw new FeatureManagementException(
                       FeatureManagementError.MissingFeatureVariantAssigner,
                       $"The feature variant assigner '{featureDefinition.Assigner}' specified for feature '{feature}' was not found.");
            }

            var context = new FeatureVariantAssignmentContext()
            {
                FeatureDefinition = featureDefinition
            };

            //
            // IFeatureVariantAssigner
            if (assigner is IFeatureVariantAssigner featureVariantAssigner)
            {
                variant = await featureVariantAssigner.AssignVariantAsync(context, cancellationToken).ConfigureAwait(false);
            }
            //
            // IContextualFeatureVariantAssigner
            else if (useAppContext &&
                     TryGetContextualFeatureVariantAssigner(featureDefinition.Assigner, typeof(TContext), out ContextualFeatureVariantAssignerEvaluator contextualAssigner))
            {
                variant = await contextualAssigner.AssignVariantAsync(context, appContext, cancellationToken).ConfigureAwait(false);
            }
            //
            // The assigner doesn't implement a feature variant assigner interface capable of performing the evaluation
            else
            {
                throw new FeatureManagementException(
                    FeatureManagementError.InvalidFeatureVariantAssigner,
                    useAppContext ?
                        $"The feature variant assigner '{featureDefinition.Assigner}' specified for the feature '{feature}' is not capable of evaluating the requested feature with the provided context." :
                        $"The feature variant assigner '{featureDefinition.Assigner}' specified for the feature '{feature}' is not capable of evaluating the requested feature.");
            }

            if (variant == null)
            {
                variant = defaultVariant;
            }

            return await _variantOptionsResolver.GetOptionsAsync<T>(featureDefinition, variant, cancellationToken).ConfigureAwait(false);
        }

        private IFeatureVariantAssignerMetadata GetFeatureVariantAssignerMetadata(string assignerName)
        {
            const string assignerSuffix = "assigner";

            IFeatureVariantAssignerMetadata assigner = _assignerMetadataCache.GetOrAdd(
                assignerName,
                (_) => {

                    IEnumerable<IFeatureVariantAssignerMetadata> matchingAssigners = _variantAssigners.Where(a =>
                    {
                        Type assignerType = a.GetType();

                        string name = ((AssignerAliasAttribute)Attribute.GetCustomAttribute(assignerType, typeof(AssignerAliasAttribute)))?.Alias;

                        if (name == null)
                        {
                            name = assignerType.Name;
                        }

                        return IsMatchingMetadataName(name, assignerName, assignerSuffix);
                    });

                    if (matchingAssigners.Count() > 1)
                    {
                        throw new FeatureManagementException(FeatureManagementError.AmbiguousFeatureVariantAssigner, $"Multiple feature variant assigners match the configured assigner named '{assignerName}'.");
                    }

                    return matchingAssigners.FirstOrDefault();
                }
            );

            return assigner;
        }

        private static bool IsMatchingMetadataName(string metadataName, string desiredName, string suffix)
        {
            //
            // Feature filters can be referenced with or without the 'filter' suffix
            // E.g. A feature can reference a filter named 'CustomFilter' as 'Custom' or 'CustomFilter'
            if (!desiredName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
                metadataName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                metadataName = metadataName.Substring(0, metadataName.Length - suffix.Length);
            }

            //
            // Feature filters can have namespaces in their alias
            // If a feature is configured to use a filter without a namespace such as 'MyFilter', then it can match 'MyOrg.MyProduct.MyFilter' or simply 'MyFilter'
            // If a feature is configured to use a filter with a namespace such as 'MyOrg.MyProduct.MyFilter' then it can only match 'MyOrg.MyProduct.MyFilter' 
            if (desiredName.Contains('.'))
            {
                //
                // The configured filter name is namespaced. It must be an exact match.
                return string.Equals(metadataName, desiredName, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                //
                // We take the simple name of a filter, E.g. 'MyFilter' for 'MyOrg.MyProduct.MyFilter'
                string simpleName = metadataName.Contains('.') ? metadataName.Split('.').Last() : metadataName;

                return string.Equals(simpleName, desiredName, StringComparison.OrdinalIgnoreCase);
            }
        }

        private bool TryGetContextualFeatureVariantAssigner(string assignerName,  Type appContextType, out ContextualFeatureVariantAssignerEvaluator assigner)
        {
            if (appContextType == null)
            {
                throw new ArgumentNullException(nameof(appContextType));
            }

            assigner = _contextualFeatureVariantAssignerCache.GetOrAdd(
                $"{assignerName}{Environment.NewLine}{appContextType.FullName}",
                (_) => {

                    IFeatureVariantAssignerMetadata metadata = GetFeatureVariantAssignerMetadata(assignerName);

                    return ContextualFeatureVariantAssignerEvaluator.IsContextualVariantAssigner(metadata, appContextType) ?
                        new ContextualFeatureVariantAssignerEvaluator(metadata, appContextType) :
                        null;
                }
            );

            return assigner != null;
        }
    }
}
