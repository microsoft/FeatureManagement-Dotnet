// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Microsoft.FeatureManagement
{
    /// <summary>
    /// Wraps an instance of IConfiguration. This allows the reference to be updated when the underlying IConfiguration is updated. 
    /// This is useful for cache busting based on the reference.
    /// </summary>
    class ConfigurationWrapper : IConfiguration
    {
        private readonly IConfiguration _configuration;

        public ConfigurationWrapper(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string this[string key]
        {
            get => _configuration[key];
            set => _configuration[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() =>
            _configuration.GetChildren();

        public IChangeToken GetReloadToken() =>
            _configuration.GetReloadToken();

        public IConfigurationSection GetSection(string key) =>
            _configuration.GetSection(key);
    }
}