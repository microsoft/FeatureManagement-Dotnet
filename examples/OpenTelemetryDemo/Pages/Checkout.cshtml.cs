// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace OpenTelemetryDemo.Pages
{
    public class CheckoutModel : PageModel
    {
        private readonly Meter _meter;
        private readonly Histogram<long> _checkoutAmountHistogram;
        private readonly ILogger<CheckoutModel> _logger;

        public CheckoutModel(IMeterFactory meterFactory, ILogger<CheckoutModel> logger)
        {
            _meter = meterFactory?.Create("OpenTelemetryDemo") ?? throw new ArgumentNullException(nameof(meterFactory));
            _checkoutAmountHistogram = _meter.CreateHistogram<long>("checkoutAmount");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool CheckedOut { get; set; }

        public int CheckoutAmount { get; set; }

        public void OnPost()
        {
            CheckoutAmount = Random.Shared.Next(1, 100);

            //
            // Track the checkout amount metric using the OpenTelemetry metrics API
            // (System.Diagnostics.Metrics), exported via WithMetrics/AddMeter.
            _checkoutAmountHistogram.Record(CheckoutAmount);

            //
            // Emits a log-based custom event, the OpenTelemetry equivalent of
            // TelemetryClient.TrackEvent. TargetingLogProcessor (wired up automatically by
            // AddFeatureManagement().AddOpenTelemetry()) enriches it with TargetingId, just like
            // the FeatureEvaluation event.
            _logger.LogCheckout(CheckoutAmount);

            CheckedOut = true;
        }
    }
}
