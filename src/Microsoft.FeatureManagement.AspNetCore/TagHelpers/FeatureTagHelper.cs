// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.Mvc.TagHelpers
{
    /// <summary>
    /// Provides a <![CDATA[<feature>]]> tag that can be used to conditionally render content based on a feature's state.
    /// </summary>
    public class FeatureTagHelper : TagHelper
    {
        private readonly IFeatureManager _featureManager;
        private readonly IVariantFeatureManager _variantFeatureManager;

        /// <summary>
        /// A feature name, or comma separated list of feature names, for which the content should be rendered. By default, all specified features must be enabled to render the content, but this requirement can be controlled by the <see cref="Requirement"/> property.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Controls whether 'All' or 'Any' feature in a list of features should be enabled to render the content within the feature tag.
        /// </summary>
        public RequirementType Requirement { get; set; } = RequirementType.All;

        /// <summary>
        /// Negates the evaluation for whether or not a feature tag should display content. This is used to display alternate content when a feature or set of features are disabled.
        /// </summary>
        public bool Negate { get; set; }

        /// <summary>
        /// A variant name, or comma separated list of variant names. If any of specified variants is assigned, the content should be rendered.
        /// If variant is specified, <see cref="Name"/> must contain only one feature name and <see cref="Requirement"/> will have no effect.
        /// </summary>
        public string Variant { get; set; }

        /// <summary>
        /// Creates a feature tag helper.
        /// </summary>
        /// <param name="featureManager">The feature manager snapshot to use to evaluate feature state.</param>
        /// <param name="variantFeatureManager">The variant feature manager snapshot to use to evaluate feature state.</param>
        public FeatureTagHelper(IFeatureManagerSnapshot featureManager, IVariantFeatureManagerSnapshot variantFeatureManager)
        {
            // Takes both a feature manager and a variant feature manager for backwards compatibility.
            _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
            _variantFeatureManager = variantFeatureManager ?? throw new ArgumentNullException(nameof(variantFeatureManager));
        }

        /// <summary>
        /// Processes the tag helper context to evaluate if the feature's content should be rendered.
        /// </summary>
        /// <param name="context">The tag helper context.</param>
        /// <param name="output">The tag helper output.</param>
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null; // We don't want the feature to actually be a part of HTML, so we strip it

            bool enabled = false;

            if (!string.IsNullOrEmpty(Name))
            {
                IEnumerable<string> features = Name.Split(',').Select(n => n.Trim());

                if (string.IsNullOrEmpty(Variant))
                {
                    enabled = Requirement == RequirementType.All
                        ? await features.All(async feature => await _featureManager.IsEnabledAsync(feature).ConfigureAwait(false))
                        : await features.Any(async feature => await _featureManager.IsEnabledAsync(feature).ConfigureAwait(false));
                }
                else
                {
                    if (features.Count() != 1)
                    {
                        throw new ArgumentException("Variant cannot be associated with multiple feature flags.", nameof(Name));
                    }

                    IEnumerable<string> variants = Variant.Split(',').Select(n => n.Trim());

                    if (variants.Count() != 1 && Requirement == RequirementType.All)
                    {
                        throw new ArgumentException("Requirement must be Any when there are multiple variants.", nameof(Requirement));
                    }

                    enabled = await variants.Any(
                        async variant =>
                        {
                            Variant assignedVariant = await _variantFeatureManager.GetVariantAsync(features.First()).ConfigureAwait(false);

                            return variant == assignedVariant?.Name;
                        });
                }
            }

            if (Negate)
            {
                enabled = !enabled;
            }

            if (!enabled)
            {
                output.SuppressOutput();
            }
        }
    }
}
