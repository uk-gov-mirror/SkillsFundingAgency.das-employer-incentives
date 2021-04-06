﻿using SFA.DAS.EmployerIncentives.Abstractions.Domain;
using System;
using System.Collections.Generic;

namespace SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives.ValueTypes
{
    public class AgreementVersion : ValueObject
    {
        private const int MinimumEmployerIncentivesAgreementVersion = 4;
        private const int SchemeEligibilityExtensionAgreementVersion = 5;
        public int MinimumRequiredVersion { get; }

        public AgreementVersion(DateTime startDate)
        {
            var schemeEligibilityExtensionStartDate = new DateTime(2021, 02, 01);

            if (startDate < schemeEligibilityExtensionStartDate)
            {
                MinimumRequiredVersion = MinimumEmployerIncentivesAgreementVersion;
            }
            else
            {
                MinimumRequiredVersion = SchemeEligibilityExtensionAgreementVersion;
            }
        }
        
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return MinimumRequiredVersion;
        }
    }
}
