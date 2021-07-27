// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Consoto.Banking.AccountService
{
    class DayOfWeekAssigner : IFeatureVariantAssigner
    {
        public ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext variantAssignmentContext, CancellationToken _)
        {
            FeatureDefinition featureDefinition = variantAssignmentContext.FeatureDefinition;

            FeatureVariant chosenVariant = null;

            string currentDay = DateTimeOffset.UtcNow.DayOfWeek.ToString();

            foreach (var variant in featureDefinition.Variants)
            {
                DayOfWeekAssignmentParameters p = variant.AssignmentParameters.Get<DayOfWeekAssignmentParameters>() ??
                                                    new DayOfWeekAssignmentParameters();

                if (!string.IsNullOrEmpty(p.DayOfWeek) &&
                    p.DayOfWeek.Equals(currentDay, StringComparison.OrdinalIgnoreCase))
                {
                    chosenVariant = variant;

                    break;
                }
            }

            return new ValueTask<FeatureVariant>(chosenVariant);
        }
    }
}
