// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Provides a way to customize feature management functionality.
    /// </summary>
    public interface IFeatureManagementBuilder
    {
        /// <summary>
        /// The application services.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Adds a given feature filter to the list of feature filters that will be available to enable features during runtime.
        /// Possible feature filter metadata types include <see cref="IFeatureFilter"/> and <see cref="IContextualFeatureFilter{TContext}"/>
        /// Only one feature filter interface can be implemented by a single type.
        /// </summary>
        /// <typeparam name="T">The feature filter type.</typeparam>
        /// <returns>The feature management builder.</returns>
        IFeatureManagementBuilder AddFeatureFilter<T>() where T : IFeatureFilterMetadata;

        /// <summary>
        /// Adds an <see cref="ISessionManager"/> to be used for storing feature state in a session.
        /// </summary>
        /// <typeparam name="T">An implementation of <see cref="ISessionManager"/></typeparam>
        /// <returns>The feature management builder.</returns>
        IFeatureManagementBuilder AddSessionManager<T>() where T : ISessionManager;

        /// <summary>
        /// Adds a given feature variant assigner to the list of feature variant assigners that will be available to assign feature variants during runtime.
        /// Possible feature variant assigner metadata types include <see cref="IFeatureVariantAssigner"/> and <see cref="IContextualFeatureVariantAssigner{TContext}"/>
        /// Only one feature variant assigner interface can be implemented by a single type.
        /// </summary>
        /// <typeparam name="T">An implementation of <see cref="IFeatureVariantAssignerMetadata"/></typeparam>
        /// <returns>The feature management builder.</returns>
        IFeatureManagementBuilder AddFeatureVariantAssigner<T>() where T : IFeatureVariantAssignerMetadata;
    }
}
