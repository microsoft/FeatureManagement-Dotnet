// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.Allocators
{
    /// <summary>
    /// A feature variant allocator that can be used to allocate a variant based on targeted audiences.
    /// </summary>
    [AllocatorAlias(Alias)]
    public class TargetingFeatureVariantAllocator : IFeatureVariantAllocator
    {
        private const string Alias = "Microsoft.Targeting";
        private readonly ITargetingContextAccessor _contextAccessor;
        private readonly IContextualFeatureVariantAllocator<ITargetingContext> _contextualResolver;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a feature variant allocator that uses targeting to allocate which of a dynamic feature's registered variants should be used.
        /// </summary>
        /// <param name="options">The options controlling how targeting is performed.</param>
        /// <param name="contextAccessor">An accessor for the targeting context required to perform a targeting evaluation.</param>
        /// <param name="loggerFactory">A logger factory for producing logs.</param>
        public TargetingFeatureVariantAllocator(IOptions<TargetingEvaluationOptions> options,
                                               ITargetingContextAccessor contextAccessor,
                                               ILoggerFactory loggerFactory)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _contextualResolver = new ContextualTargetingFeatureVariantAllocator(options);
            _logger = loggerFactory?.CreateLogger<TargetingFeatureVariantAllocator>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Allocates one of the variants configured for a feature based off the provided targeting context.
        /// </summary>
        /// <param name="variantAllocationContext">Contextual information available for use during the allocation process.</param>
        /// <param name="isFeatureEnabled">A boolean indicating whether the feature the variant is being allocated to is enabled.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns></returns>
        public async ValueTask<FeatureVariant> AllocateVariantAsync(FeatureVariantAllocationContext variantAllocationContext, bool isFeatureEnabled, CancellationToken cancellationToken)
        {
            if (variantAllocationContext == null)
            {
                throw new ArgumentNullException(nameof(variantAllocationContext));
            }

            //
            // Acquire targeting context via accessor
            TargetingContext targetingContext = await _contextAccessor.GetContextAsync(cancellationToken).ConfigureAwait(false);

            //
            // Ensure targeting can be performed
            if (targetingContext == null)
            {
                _logger.LogWarning("No targeting context available for targeting evaluation.");

                return null;
            }

            return await _contextualResolver.AllocateVariantAsync(variantAllocationContext, targetingContext, isFeatureEnabled, cancellationToken).ConfigureAwait(false);
        }
    }
}