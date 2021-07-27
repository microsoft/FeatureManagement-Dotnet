// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement.Targeting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// A feature variant assigner that can be used to assign a variant based on targeted audiences.
    /// </summary>
    [FilterAlias(Alias)]
    public class ContextualTargetingFeatureVariantAssigner : IContextualFeatureVariantAssigner<ITargetingContext>
    {
        private const string Alias = "Microsoft.Targeting";

        /// <summary>
        /// Assigns one of the variants configured for a feature based off the provided targeting context.
        /// </summary>
        /// <param name="variantAssignmentContext">Contextual information available for use during the assignment process.</param>
        /// <param name="targetingContext">The targeting context used to determine which variant should be assigned.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns></returns>
        public ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext variantAssignmentContext, ITargetingContext targetingContext, CancellationToken cancellationToken)
        {
            FeatureDefinition featureDefinition = variantAssignmentContext.FeatureDefinition;

            if (featureDefinition == null)
            {
                return new ValueTask<FeatureVariant>((FeatureVariant)null);
            }

            FeatureVariant variant = null;

            FeatureVariant defaultVariant = null;

            double cumulativePercentage = 0;

            var cumulativeGroups = new Dictionary<string, double>();

            if (featureDefinition.Variants != null)
            {
                foreach (FeatureVariant v in featureDefinition.Variants)
                {
                    if (defaultVariant == null && v.Default)
                    {
                        defaultVariant = v;
                    }

                    TargetingFilterSettings targetingSettings = v.AssignmentParameters.Get<TargetingFilterSettings>();

                    if (targetingSettings == null)
                    {
                        //
                        // Valid to omit audience for default variant
                        continue;
                    }

                    AccumulateAudience(targetingSettings.Audience, ref cumulativePercentage, ref cumulativeGroups);

                    if (TargetingEvaluator.IsTargeted(targetingSettings, targetingContext, true, featureDefinition.Name))
                    {
                        variant = v;

                        break;
                    }
                }
            }

            if (variant == null)
            {
                variant = defaultVariant;
            }

            return new ValueTask<FeatureVariant>(variant);
        }

        private static void AccumulateAudience(Audience audience, ref double cumulativePercentage, ref Dictionary<string, double> cumulativeGroups)
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

            cumulativePercentage = cumulativePercentage + audience.DefaultRolloutPercentage;
        }
    }
}
