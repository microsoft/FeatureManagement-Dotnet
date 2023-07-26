// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement.Targeting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.Allocators
{
    /// <summary>
    /// A feature variant allocator that can be used to allocate a variant based on targeted audiences.
    /// </summary>
    [AllocatorAlias(Alias)]
    public class ContextualTargetingFeatureVariantAllocator : IContextualFeatureVariantAllocator<ITargetingContext>
    {
        private const string Alias = "Microsoft.Targeting";
        private readonly TargetingEvaluationOptions _options;

        /// <summary>
        /// Creates a targeting contextual feature filter.
        /// </summary>
        /// <param name="options">Options controlling the behavior of the targeting evaluation performed by the filter.</param>
        public ContextualTargetingFeatureVariantAllocator(IOptions<TargetingEvaluationOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Allocates one of the variants configured for a feature based off the provided targeting context.
        /// </summary>
        /// <param name="variantAllocationContext">Contextual information available for use during the allocation process.</param>
        /// <param name="targetingContext">The targeting context used to determine which variant should be allocated.</param>
        /// <param name="isFeatureEnabled">A boolean indicating whether the feature the variant is being allocated to is enabled.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns></returns>
        public ValueTask<FeatureVariant> AllocateVariantAsync(FeatureVariantAllocationContext variantAllocationContext, ITargetingContext targetingContext, bool isFeatureEnabled, CancellationToken cancellationToken)
        {
            if (variantAllocationContext == null)
            {
                throw new ArgumentNullException(nameof(variantAllocationContext));
            }

            if (targetingContext == null)
            {
                throw new ArgumentNullException(nameof(targetingContext));
            }

            FeatureDefinition featureDefinition = variantAllocationContext.FeatureDefinition;

            if (featureDefinition == null)
            {
                throw new ArgumentException(
                    $"{nameof(variantAllocationContext)}.{nameof(variantAllocationContext.FeatureDefinition)} cannot be null.",
                    nameof(variantAllocationContext));
            }

            if (featureDefinition.Variants == null)
            {
                throw new ArgumentException(
                    $"{nameof(variantAllocationContext)}.{nameof(variantAllocationContext.FeatureDefinition)}.{nameof(featureDefinition.Variants)} cannot be null.",
                    nameof(variantAllocationContext));
            }

            FeatureVariant variant = null;

            if (!isFeatureEnabled)
            {
                if (!string.IsNullOrEmpty(featureDefinition.Allocation.DefaultWhenDisabled))
                {
                    variant = featureDefinition.Variants.FirstOrDefault((variant) => variant.Name.Equals(featureDefinition.Allocation.DefaultWhenDisabled));

                    if (!string.IsNullOrEmpty(variant.Name))
                    {
                        return new ValueTask<FeatureVariant>(variant);
                    }
                }

                return new ValueTask<FeatureVariant>((FeatureVariant)null);
            }

            foreach (User user in featureDefinition.Allocation.User)
            {
                if (TargetingEvaluator.IsTargeted(targetingContext, user.Users, _options.IgnoreCase))
                {
                    variant = featureDefinition.Variants.FirstOrDefault((variant) => variant.Name.Equals(user.Variant));

                    if (!string.IsNullOrEmpty(variant.Name))
                    {
                        return new ValueTask<FeatureVariant>(variant);
                    }
                }
            }

            foreach (Group group in featureDefinition.Allocation.Group)
            {
                if (TargetingEvaluator.IsGroupTargeted(targetingContext, group.Groups, _options.IgnoreCase))
                {
                    variant = featureDefinition.Variants.FirstOrDefault((variant) => variant.Name.Equals(group.Variant));

                    if (!string.IsNullOrEmpty(variant.Name))
                    {
                        return new ValueTask<FeatureVariant>(variant);
                    }
                }
            }

            // what to do if seed not specified? random int? TODO
            foreach (Percentile percentile in featureDefinition.Allocation.Percentile)
            {
                if (TargetingEvaluator.IsTargeted(targetingContext, percentile.From, percentile.To, featureDefinition.Allocation.Seed, _options.IgnoreCase, featureDefinition.Name))
                {
                    variant = featureDefinition.Variants.FirstOrDefault((variant) => variant.Name.Equals(percentile.Variant));

                    if (!string.IsNullOrEmpty(variant.Name))
                    {
                        return new ValueTask<FeatureVariant>(variant);
                    }
                }
            }

            if (!string.IsNullOrEmpty(featureDefinition.Allocation.DefaultWhenEnabled))
            {
                variant = featureDefinition.Variants.FirstOrDefault((variant) => variant.Name.Equals(featureDefinition.Allocation.DefaultWhenEnabled));

                if (!string.IsNullOrEmpty(variant.Name))
                {
                    return new ValueTask<FeatureVariant>(variant);
                }
            }

            return new ValueTask<FeatureVariant>(variant);
        }
    }
}