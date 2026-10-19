// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Xunit;

// ActivityListener registrations from FeatureEvaluationEventPublisher are process-global (via
// ActivitySource.AddActivityListener), so tests must not run concurrently or they may observe
// each other's activities/events.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
