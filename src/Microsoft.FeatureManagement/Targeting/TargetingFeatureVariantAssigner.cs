// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.FeatureFilters;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.Assigners
{
    /// <summary>
    /// A feature variant assigner that can be used to assign a variant based on targeted audiences.
    /// </summary>
    [AssignerAlias(Alias)]
    public class TargetingFeatureVariantAssigner : IFeatureVariantAssigner
    {
        private const string Alias = "Microsoft.Targeting";
        private readonly ITargetingContextAccessor _contextAccessor;
        private readonly IContextualFeatureVariantAssigner<ITargetingContext> _contextualResolver;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a feature variant assigner that uses targeting to assign which of a feature's registered variants should be used.
        /// </summary>
        /// <param name="options">The options controlling how targeting is performed.</param>
        /// <param name="contextAccessor">An accessor for the targeting context required to perform a targeting evaluation.</param>
        /// <param name="loggerFactory">A logger factory for producing logs.</param>
        public TargetingFeatureVariantAssigner(IOptions<TargetingEvaluationOptions> options,
                                               ITargetingContextAccessor contextAccessor,
                                               ILoggerFactory loggerFactory)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _contextualResolver = new ContextualTargetingFeatureVariantAssigner(options);
            _logger = loggerFactory?.CreateLogger<TargetingFeatureVariantAssigner>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Assigns one of the variants configured for a feature based off the provided targeting context.
        /// </summary>
        /// <param name="variantAssignmentContext">Contextual information available for use during the assignment process.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns></returns>
        public async ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext variantAssignmentContext, CancellationToken cancellationToken)
        {
            if (variantAssignmentContext == null)
            {
                throw new ArgumentNullException(nameof(variantAssignmentContext));
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

            return await _contextualResolver.AssignVariantAsync(variantAssignmentContext, targetingContext, cancellationToken).ConfigureAwait(false);
        }
    }
}
