// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.FeatureManagement.Mvc.RazorPages
{
    /// <summary>
    /// An attribute that can be placed on razor pages to require all or any of a set of features to be enabled.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PageFeatureGateAttribute : Attribute, IAsyncPageFilter
    {
        /// <summary>
        /// Creates an attribute that will gate razor pages unless all the provided feature(s) are enabled.
        /// </summary>
        /// <param name="features">The names of the features that the attribute will represent.</param>
        public PageFeatureGateAttribute(params string[] features)
            : this(RequirementType.All, features)
        {
        }

        /// <summary>
        /// Creates an attribute that can be used to gate razor pages. The gate can be configured to require all or any of the provided feature(s) to pass.
        /// </summary>
        /// <param name="requirementType">Specifies whether all or any of the provided features should be enabled in order to pass.</param>
        /// <param name="features">The names of the features that the attribute will represent.</param>
        public PageFeatureGateAttribute(RequirementType requirementType, params string[] features)
        {
            if (features == null || features.Length == 0)
            {
                throw new ArgumentNullException(nameof(features));
            }

            Features = features;

            RequirementType = requirementType;
        }

        /// <summary>
        /// Creates an attribute that will gate razor pages unless all the provided feature(s) are enabled.
        /// </summary>
        /// <param name="features">A set of enums representing the features that the attribute will represent.</param>
        public PageFeatureGateAttribute(params object[] features)
            : this(RequirementType.All, features)
        {
        }

        /// <summary>
        /// Creates an attribute that can be used to gate razor pages. The gate can be configured to require all or any of the provided feature(s) to pass.
        /// </summary>
        /// <param name="requirementType">Specifies whether all or any of the provided features should be enabled in order to pass.</param>
        /// <param name="features">A set of enums representing the features that the attribute will represent.</param>
        public PageFeatureGateAttribute(RequirementType requirementType, params object[] features)
        {
            if (features == null || features.Length == 0)
            {
                throw new ArgumentNullException(nameof(features));
            }

            var fs = new List<string>();

            foreach (object feature in features)
            {
                var type = feature.GetType();

                if (!type.IsEnum)
                {
                    // invalid
                    throw new ArgumentException("The provided features must be enums.", nameof(features));
                }

                fs.Add(Enum.GetName(feature.GetType(), feature));
            }

            Features = fs;

            RequirementType = requirementType;
        }

        /// <summary>
        /// The name of the features that the feature attribute will activate for.
        /// </summary>
        public IEnumerable<string> Features { get; }

        /// <summary>
        /// Controls whether any or all features in <see cref="Features"/> should be enabled to pass.
        /// </summary>
        public RequirementType RequirementType { get; }

        /// <summary>
        /// Called asynchronously before the handler method is invoked, after model binding is complete.
        /// </summary>
        /// <param name="context">The <see cref="PageHandlerExecutingContext"/>.</param>
        /// <param name="next">The <see cref="PageHandlerExecutionDelegate"/>. Invoked to execute the next page filter or the handler method itself.</param>
        /// <returns>A <see cref="Task"/> that on completion indicates the filter has executed.</returns>
        public virtual async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            IFeatureManagerSnapshot fm = context.HttpContext.RequestServices.GetRequiredService<IFeatureManagerSnapshot>();

            //
            // Enabled state is determined by either 'any' or 'all' features being enabled.
            bool enabled = RequirementType == RequirementType.All ?
                             await Features.All(async feature => await fm.IsEnabledAsync(feature).ConfigureAwait(false)) :
                             await Features.Any(async feature => await fm.IsEnabledAsync(feature).ConfigureAwait(false));

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
        public virtual Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;
    }
}
