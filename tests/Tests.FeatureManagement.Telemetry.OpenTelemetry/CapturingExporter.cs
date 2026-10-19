// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using OpenTelemetry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tests.FeatureManagement.Telemetry.OpenTelemetry
{
    internal sealed class CapturingExporter<T> : BaseExporter<T> where T : class
    {
        private readonly Func<T, IEnumerable<KeyValuePair<string, object>>> _getAttributes;

        public List<IReadOnlyList<KeyValuePair<string, object>>> Records { get; } = new List<IReadOnlyList<KeyValuePair<string, object>>>();

        public CapturingExporter(Func<T, IEnumerable<KeyValuePair<string, object>>> getAttributes)
        {
            _getAttributes = getAttributes;
        }

        public override ExportResult Export(in Batch<T> batch)
        {
            foreach (T item in batch)
            {
                // Snapshot attributes before any later processor can mutate the telemetry.
                Records.Add((_getAttributes(item) ?? Array.Empty<KeyValuePair<string, object>>()).ToArray());
            }

            return ExportResult.Success;
        }
    }
}
