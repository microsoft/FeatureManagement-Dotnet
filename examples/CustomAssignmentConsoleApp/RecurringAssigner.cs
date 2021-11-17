// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Consoto.Banking.AccountService
{
    [AssignerAlias("Recurring")]
    class RecurringAssigner : IFeatureVariantAssigner
    {
        public ValueTask<FeatureVariant> AssignVariantAsync(FeatureVariantAssignmentContext variantAssignmentContext, CancellationToken _)
        {
            FeatureDefinition featureDefinition = variantAssignmentContext.FeatureDefinition;

            FeatureVariant chosenVariant = null;

            string currentDay = DateTimeOffset.UtcNow.DayOfWeek.ToString();

            foreach (var variant in featureDefinition.Variants)
            {
                RecurringAssignmentParameters p = variant.AssignmentParameters.Get<RecurringAssignmentParameters>() ??
                                                    new RecurringAssignmentParameters();

                if (p.Days != null &&
                    p.Days.Any(d => d.Equals(currentDay, StringComparison.OrdinalIgnoreCase)))
                {
                    chosenVariant = variant;

                    break;
                }
            }

            return new ValueTask<FeatureVariant>(chosenVariant);
        }
    }
}
