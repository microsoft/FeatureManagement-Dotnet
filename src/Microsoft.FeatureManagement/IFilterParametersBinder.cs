// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// An interface used by the feature management system to pre-bind feature filter parameters to a settings type.
    /// <see cref="IFeatureFilter"/>s can implement this interface to take advantage of caching of settings by the feature management system.
    /// </summary>
    public interface IFilterParametersBinder
    {
        /// <summary>
        /// Binds a set of feature filter parameters to a settings object.
        /// </summary>
        /// <param name="parameters">The configuration representing filter parameters to bind to a settings object</param>
        /// <returns>A settings object that is understood by the implementer of <see cref="IFilterParametersBinder"/>.</returns>
        object BindParameters(IConfiguration parameters);
    }
}