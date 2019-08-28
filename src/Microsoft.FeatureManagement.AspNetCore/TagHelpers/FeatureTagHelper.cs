// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Razor.TagHelpers;
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
        /// Creates a feature tag helper.
        /// </summary>
        /// <param name="featureManager">The feature manager snapshot to use to evaluate feature state.</param>
        public FeatureTagHelper(IFeatureManagerSnapshot featureManager)
        {
            _featureManager = featureManager;
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
                IEnumerable<string> names = Name.Split(',').Select(n => n.Trim());

                enabled = Requirement == RequirementType.All ?
                    await names.All(async n => await _featureManager.IsEnabledAsync(n).ConfigureAwait(false)) :
                    await names.Any(async n => await _featureManager.IsEnabledAsync(n).ConfigureAwait(false));
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
