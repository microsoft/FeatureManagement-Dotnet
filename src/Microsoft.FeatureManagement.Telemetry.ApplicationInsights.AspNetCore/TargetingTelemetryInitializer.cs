﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//

using Microsoft.ApplicationInsights.AspNetCore.TelemetryInitializers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;

namespace Microsoft.FeatureManagement.Telemetry.ApplicationInsights.AspNetCore
{
    /// <summary>
    /// Used to add targeting information to outgoing Application Insights telemetry.
    /// </summary>
    public class TargetingTelemetryInitializer : TelemetryInitializerBase
    {
        /// <summary>
        /// Creates an instance of the TargetingTelemetryInitializer
        /// </summary>
        public TargetingTelemetryInitializer(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
        }

        /// <summary>
        /// When telemetry is initialized, adds targeting information to all relevant telemetry.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> to get the targeting information from.</param>
        /// <param name="requestTelemetry">The <see cref="RequestTelemetry"/> relevant to the telemetry.</param>
        /// <param name="telemetry">The <see cref="ITelemetry"/> to be initialized.</param>
        /// <exception cref="ArgumentNullException">Thrown if the any param is null.</exception>
        protected override void OnInitializeTelemetry(HttpContext httpContext, RequestTelemetry requestTelemetry, ITelemetry telemetry)
        {
            if (telemetry == null)
            {
                throw new ArgumentNullException(nameof(telemetry));
            }

            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            // Extract the targeting id from the request telemetry
            if (requestTelemetry.Properties.ContainsKey(Constants.TargetingIdKey))
            {
                // Telemetry.Properties is deprecated in favor of ISupportProperties
                if (telemetry is ISupportProperties telemetryWithSupportProperties)
                {
                    telemetryWithSupportProperties.Properties[Constants.TargetingIdKey] = requestTelemetry.Properties[Constants.TargetingIdKey];
                }
            }
        }
    }
}
