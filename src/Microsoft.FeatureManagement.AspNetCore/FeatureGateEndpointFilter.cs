// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.AspNetCore
{
    /// <summary>
    /// An endpoint filter that controls access based on feature flag states.
    /// </summary>
    internal sealed class FeatureGateEndpointFilter : IEndpointFilter
    {
        /// <summary>
        /// Gets the collection of feature flags to evaluate.
        /// </summary>
        private readonly IEnumerable<string> _features;

        /// <summary>
        /// Gets the type of requirement (All or Any) for feature evaluation.
        /// </summary>
        private readonly RequirementType _requirementType;

        /// <summary>
        /// Gets whether the feature evaluation result should be negated.
        /// </summary>
        private readonly bool _negate;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureGateEndpointFilter"/> class.
        /// </summary>
        /// <param name="features">The collection of feature flags to evaluate.</param>
        /// <exception cref="ArgumentNullException">Thrown when features collection is null or empty.</exception>
        public FeatureGateEndpointFilter(params string[] features)
            : this(RequirementType.All, negate: false, features)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FeatureGateEndpointFilter"/> class.
        /// </summary>
        /// <param name="requirementType">Specifies whether all or any of the provided features should be enabled in order to pass.</param>
        /// <param name="features">The collection of feature flags to evaluate.</param>
        /// <exception cref="ArgumentNullException">Thrown when features collection is null or empty.</exception>
        public FeatureGateEndpointFilter(RequirementType requirementType, params string[] features)
            : this(requirementType, negate: false, features)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FeatureGateEndpointFilter"/> class.
        /// </summary>
        /// <param name="requirementType">Specifies whether all or any of the provided features should be enabled in order to pass.</param>
        /// <param name="negate">Specifies whether the feature evaluation result should be negated.</param>
        /// <param name="features">The collection of feature flags to evaluate.</param>
        /// <exception cref="ArgumentNullException">Thrown when features collection is null or empty.</exception>
        public FeatureGateEndpointFilter(RequirementType requirementType, bool negate, params string[] features)
        {
            if (features == null || features.Length == 0)
            {
                throw new ArgumentNullException(nameof(features));
            }

            _features = features;
            _requirementType = requirementType;
            _negate = negate;
        }

        /// <summary>
        /// Invokes the feature flag filter to control endpoint access based on feature states.
        /// </summary>
        /// <param name="context">The endpoint filter invocation context.</param>
        /// <param name="next">The delegate representing the next filter in the pipeline.</param>
        /// <returns>
        /// A <see cref="NotFound"/> result if access is denied, otherwise continues the pipeline.
        /// </returns>
        public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            IVariantFeatureManager fm = context.HttpContext.RequestServices.GetRequiredService<IVariantFeatureManagerSnapshot>();

            bool enabled = _requirementType == RequirementType.All
                ? await _features.All(async feature => await fm.IsEnabledAsync(feature).ConfigureAwait(false))
                : await _features.Any(async feature => await fm.IsEnabledAsync(feature).ConfigureAwait(false));

            bool isAllowed = _negate ? !enabled : enabled;

            return isAllowed
                ? await next(context).ConfigureAwait(false)
                : Results.NotFound();
        }
    }
}
