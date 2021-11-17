// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement.Targeting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A feature variant assigner that can be used to assign a variant based on targeted audiences.
    /// </summary>
    [AssignerAlias(Alias)]
    public class ContextualTargetingFeatureVariantAssigner : IContextualFeatureVariantAssigner<ITargetingContext>
    {
        private const string Alias = "Microsoft.Targeting";
        private readonly TargetingEvaluationOptions _options;

        /// <summary>
        /// Creates a targeting contextual feature filter.
        /// </summary>
        /// <param name="options">Options controlling the behavior of the targeting evaluation performed by the filter.</param>
        public ContextualTargetingFeatureVariantAssigner(IOptions<TargetingEvaluationOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Assigns one of the variants configured for a feature based off the provided targeting context.
        /// </summary>
        /// <param name="variantAssignmentContext">Contextual information available for use during the assignment process.</param>
        /// <param name="targetingContext">The targeting context used to determine which variant should be assigned.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns></returns>
        public ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext variantAssignmentContext, ITargetingContext targetingContext, CancellationToken cancellationToken)
        {
            if (variantAssignmentContext == null)
            {
                throw new ArgumentNullException(nameof(variantAssignmentContext));
            }

            if (targetingContext == null)
            {
                throw new ArgumentNullException(nameof(targetingContext));
            }

            FeatureDefinition featureDefinition = variantAssignmentContext.FeatureDefinition;

            if (featureDefinition == null)
            {
                throw new ArgumentException(
                    $"{nameof(variantAssignmentContext)}.{nameof(variantAssignmentContext.FeatureDefinition)} cannot be null.",
                    nameof(variantAssignmentContext));
            }

            if (featureDefinition.Variants == null)
            {
                throw new ArgumentException(
                    $"{nameof(variantAssignmentContext)}.{nameof(variantAssignmentContext.FeatureDefinition)}.{nameof(featureDefinition.Variants)} cannot be null.",
                    nameof(variantAssignmentContext));
            }

            FeatureVariant variant = null;

            double cumulativePercentage = 0;

            var cumulativeGroups = new Dictionary<string, double>(
                _options.IgnoreCase ? StringComparer.OrdinalIgnoreCase :
                                      StringComparer.Ordinal);

            foreach (FeatureVariant v in featureDefinition.Variants)
            {
                TargetingFilterSettings targetingSettings = v.AssignmentParameters.Get<TargetingFilterSettings>();

                if (targetingSettings == null)
                {
                    if (v.Default)
                    {
                        //
                        // Valid to omit audience for default variant
                        continue;
                    }
                }

                if (!TargetingEvaluator.TryValidateSettings(targetingSettings, out string paramName, out string reason))
                {
                    throw new ArgumentException(reason, paramName);
                }

                AccumulateAudience(targetingSettings.Audience, cumulativeGroups, ref cumulativePercentage);

                if (TargetingEvaluator.IsTargeted(targetingSettings, targetingContext, _options.IgnoreCase, featureDefinition.Name))
                {
                    variant = v;

                    break;
                }
            }

            return new ValueTask<FeatureVariant>(variant);
        }

        /// <summary>
        /// Accumulates percentages for groups and the default rollout for an audience.
        /// </summary>
        /// <param name="audience">The audience that will have its percentages updated based on currently accumulated percentages</param>
        /// <param name="cumulativeDefaultPercentage">The current cumulative default rollout percentage</param>
        /// <param name="cumulativeGroups">The current cumulative rollout percentage for each group</param>
        private static void AccumulateAudience(Audience audience, Dictionary<string, double> cumulativeGroups, ref double cumulativeDefaultPercentage)
        {
            if (audience.Groups != null)
            {
                foreach (GroupRollout gr in audience.Groups)
                {
                    double percentage = gr.RolloutPercentage;

                    if (cumulativeGroups.TryGetValue(gr.Name, out double p))
                    {
                        percentage += p;
                    }

                    cumulativeGroups[gr.Name] = percentage;

                    gr.RolloutPercentage = percentage;
                }
            }

            cumulativeDefaultPercentage = cumulativeDefaultPercentage + audience.DefaultRolloutPercentage;

            audience.DefaultRolloutPercentage = cumulativeDefaultPercentage;
        }
    }
}
