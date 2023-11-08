// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace EvaluationDataToApplicationInsights.Telemetry
{
    public class MyTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MyTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public void Initialize(ITelemetry telemetry)
        {
            HttpContext httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                return;
            }

            string username = httpContext.Request.Cookies["username"];

            if (username != null)
            {
                telemetry.Context.User.AuthenticatedUserId = username;
            }
        }
    }
}
