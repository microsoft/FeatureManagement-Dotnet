// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.Mvc
{
    /// <summary>
    /// An attribute that can be placed on MVC actions to require all or any of a set of feature flags to be enabled. If none of the feature flags are enabled, the registered <see cref="IDisabledFeaturesHandler"/> will be invoked.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class FeatureGateAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Creates an attribute that will gate actions unless all the provided feature flag(s) are enabled.
        /// </summary>
        /// <param name="featureFlags">The names of the feature flags that the attribute will represent.</param>
        public FeatureGateAttribute(params string[] featureFlags)
            : this(RequirementType.All, featureFlags)
        {
        }

        /// <summary>
        /// Creates an attribute that can be used to gate actions. The gate can be configured to require all or any of the provided feature flag(s) to pass.
        /// </summary>
        /// <param name="requirementType">Specifies whether all or any of the provided feature flags should be enabled in order to pass.</param>
        /// <param name="featureFlags">The names of the feature flags that the attribute will represent.</param>
        public FeatureGateAttribute(RequirementType requirementType, params string[] featureFlags)
        {
            if (featureFlags == null || featureFlags.Length == 0)
            {
                throw new ArgumentNullException(nameof(featureFlags));
            }

            FeatureFlags = featureFlags;

            RequirementType = requirementType;
        }

        /// <summary>
        /// Creates an attribute that will gate actions unless all the provided feature flag(s) are enabled.
        /// </summary>
        /// <param name="features">A set of enums representing the feature flags that the attribute will represent.</param>
        public FeatureGateAttribute(params object[] features)
            : this(RequirementType.All, features)
        {
        }

        /// <summary>
        /// Creates an attribute that can be used to gate actions. The gate can be configured to require all or any of the provided feature flag(s) to pass.
        /// </summary>
        /// <param name="requirementType">Specifies whether all or any of the provided feature flags should be enabled in order to pass.</param>
        /// <param name="featureFlags">A set of enums representing the feature flags that the attribute will represent.</param>
        public FeatureGateAttribute(RequirementType requirementType, params object[] featureFlags)
        {
            if (featureFlags == null || featureFlags.Length == 0)
            {
                throw new ArgumentNullException(nameof(featureFlags));
            }

            var fs = new List<string>();

            foreach (object feature in featureFlags)
            {
                var type = feature.GetType();

                if (!type.IsEnum)
                {
                    // invalid
                    throw new ArgumentException("The provided feature flags must be enums.", nameof(featureFlags));
                }

                fs.Add(Enum.GetName(feature.GetType(), feature));
            }

            FeatureFlags = fs;

            RequirementType = requirementType;
        }

        /// <summary>
        /// The name of the feature flags that the feature gate attribute will activate for.
        /// </summary>
        public IEnumerable<string> FeatureFlags { get; }

        /// <summary>
        /// Controls whether any or all feature flags in <see cref="FeatureFlags"/> should be enabled to pass.
        /// </summary>
        public RequirementType RequirementType { get; }

        /// <summary>
        /// Performs controller action pre-procesing to ensure that at least one of the specified feature flags are enabled.
        /// </summary>
        /// <param name="context">The context of the MVC action.</param>
        /// <param name="next">The action delegate.</param>
        /// <returns>Returns a task representing the action execution unit of work.</returns>
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            IFeatureManagerSnapshot fm = context.HttpContext.RequestServices.GetRequiredService<IFeatureManagerSnapshot>();

            //
            // Enabled state is determined by either 'any' or 'all' feature flags being enabled.
            bool enabled = RequirementType == RequirementType.All ?
                             await FeatureFlags.All(async feature => await fm.IsEnabledAsync(feature, context.HttpContext.RequestAborted).ConfigureAwait(false)).ConfigureAwait(false) :
                             await FeatureFlags.Any(async feature => await fm.IsEnabledAsync(feature, context.HttpContext.RequestAborted).ConfigureAwait(false)).ConfigureAwait(false);

            if (enabled)
            {
                await next().ConfigureAwait(false);
            }
            else
            {
                IDisabledFeaturesHandler disabledFeaturesHandler = context.HttpContext.RequestServices.GetService<IDisabledFeaturesHandler>() ?? new NotFoundDisabledFeaturesHandler();

                await disabledFeaturesHandler.HandleDisabledFeatures(FeatureFlags, context).ConfigureAwait(false);
            }
        }
    }
}
