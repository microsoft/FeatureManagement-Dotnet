// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.FeatureManagement.Mvc
{
    /// <summary>
    /// An attribute that can be placed on MVC controllers, controller actions, or Razor pages to require all or any of a set of feature flags to be enabled.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class FeatureGateAttribute : ActionFilterAttribute, IAsyncPageFilter
    {
        /// <summary>
        /// Creates an attribute that will gate actions or pages unless all the provided feature flag(s) are enabled.
        /// </summary>
        /// <param name="featureFlags">The names of the feature flags that the attribute will represent.</param>
        public FeatureGateAttribute(params string[] featureFlags)
            : this(RequirementType.All, featureFlags)
        {
        }

        /// <summary>
        /// Creates an attribute that can be used to gate actions or pages. The gate can be configured to require all or any of the provided feature flag(s) to pass.
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
        /// Creates an attribute that will gate actions or pages unless all the provided feature flag(s) are enabled.
        /// </summary>
        /// <param name="features">A set of enums representing the feature flags that the attribute will represent.</param>
        public FeatureGateAttribute(params object[] features)
            : this(RequirementType.All, features)
        {
        }

        /// <summary>
        /// Creates an attribute that can be used to gate actions or pages. The gate can be configured to require all or any of the provided feature flag(s) to pass.
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

        /// <summary>
        /// Called asynchronously before the handler method is invoked, after model binding is complete.
        /// </summary>
        /// <param name="context">The <see cref="PageHandlerExecutingContext"/>.</param>
        /// <param name="next">The <see cref="PageHandlerExecutionDelegate"/>. Invoked to execute the next page filter or the handler method itself.</param>
        /// <returns>A <see cref="Task"/> that on completion indicates the filter has executed.</returns>
        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            IFeatureManagerSnapshot fm = context.HttpContext.RequestServices.GetRequiredService<IFeatureManagerSnapshot>();

            //
            // Enabled state is determined by either 'any' or 'all' features being enabled.
            bool enabled = RequirementType == RequirementType.All ?
                             await FeatureFlags.All(async feature => await fm.IsEnabledAsync(feature).ConfigureAwait(false)) :
                             await FeatureFlags.Any(async feature => await fm.IsEnabledAsync(feature).ConfigureAwait(false));

            if (enabled)
            {
                await next.Invoke().ConfigureAwait(false);
            }
            else
            {
                context.Result = new NotFoundResult();
            }
        }

        /// <summary>
        /// Called asynchronously after the handler method has been selected, but before model binding occurs.
        /// </summary>
        /// <param name="context">The <see cref="PageHandlerSelectedContext"/>.</param>
        /// <returns>A <see cref="Task"/> that on completion indicates the filter has executed.</returns>
        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;
    }
}
