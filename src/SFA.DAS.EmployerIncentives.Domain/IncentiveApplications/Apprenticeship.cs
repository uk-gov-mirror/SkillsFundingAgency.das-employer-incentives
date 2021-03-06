using System;
using SFA.DAS.Common.Domain.Types;
using SFA.DAS.EmployerIncentives.Abstractions.Domain;
using SFA.DAS.EmployerIncentives.Domain.Extensions;
using SFA.DAS.EmployerIncentives.Domain.IncentiveApplications.Models;
using SFA.DAS.EmployerIncentives.Domain.ValueObjects;
using SFA.DAS.EmployerIncentives.Enums;
using SFA.DAS.EmployerIncentives.ValueObjects;

namespace SFA.DAS.EmployerIncentives.Domain.IncentiveApplications
{
    public class Apprenticeship : Entity<Guid, ApprenticeshipModel>
    {
        private const decimal TwentyFiveOrOverIncentiveAmount = 2000;
        private const decimal UnderTwentyFiveIncentiveAmount = 1500;

        public long ApprenticeshipId => Model.ApprenticeshipId;
        public string FirstName => Model.FirstName;
        public string LastName => Model.LastName;
        public DateTime DateOfBirth => Model.DateOfBirth;
        public long ULN => Model.ULN;
        public DateTime PlannedStartDate => Model.PlannedStartDate;
        public ApprenticeshipEmployerType ApprenticeshipEmployerTypeOnApproval => Model.ApprenticeshipEmployerTypeOnApproval;
        public decimal TotalIncentiveAmount => Model.TotalIncentiveAmount;
        public long? UKPRN => Model.UKPRN;
        public bool EarningsCalculated => Model.EarningsCalculated;
        public bool WithdrawnByEmployer => Model.WithdrawnByEmployer;
        public bool WithdrawnByCompliance => Model.WithdrawnByCompliance;
        public string CourseName => Model.CourseName;

        public static Apprenticeship Create(ApprenticeshipModel model)
        {
            return new Apprenticeship(model.Id, model, false);
        }

        internal Apprenticeship(Guid id, long apprenticeshipId, string firstName, string lastName, DateTime dateOfBirth, long uln, DateTime plannedStartDate, ApprenticeshipEmployerType apprenticeshipEmployerTypeOnApproval, long? ukprn, string courseName)
        {
            IsNew = false;
            Model = new ApprenticeshipModel
            {
                Id = id,
                ApprenticeshipId = apprenticeshipId,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth,
                ULN = uln,
                PlannedStartDate = plannedStartDate,
                ApprenticeshipEmployerTypeOnApproval = apprenticeshipEmployerTypeOnApproval,
                TotalIncentiveAmount = CalculateTotalIncentiveAmount(dateOfBirth, plannedStartDate),
                UKPRN = ukprn,
                CourseName = courseName
            };
        }

        public void SetEarningsCalculated(bool isCalculated = true)
        {
            Model.EarningsCalculated = isCalculated;
        }

        public void Withdraw(IncentiveApplicationStatus incentiveApplicationStatus)
        {
            switch (incentiveApplicationStatus)
            {
                case IncentiveApplicationStatus.EmployerWithdrawn:
                    Model.WithdrawnByEmployer = true;
                    break;

                case IncentiveApplicationStatus.ComplianceWithdrawn:
                    Model.WithdrawnByCompliance = true;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported IncentiveApplicationStatus:{incentiveApplicationStatus} for withdrawal");
            }
        }

        public void SetPlannedStartDate(DateTime plannedStartDate)
        {
            Model.PlannedStartDate = plannedStartDate;
        }

        private Apprenticeship(Guid id, ApprenticeshipModel model, bool isNew) : base(id, model, isNew)
        {
        }

        private decimal CalculateTotalIncentiveAmount(DateTime apprenticeDateOfBirth, DateTime plannedStartDate)
        {
            var apprenticeAge = CalculateAgeAtStartOfApprenticeship(apprenticeDateOfBirth, plannedStartDate);

            if (apprenticeAge > 24)
            {
                return UnderTwentyFiveIncentiveAmount;
            }

            return TwentyFiveOrOverIncentiveAmount;
        }

        private static int CalculateAgeAtStartOfApprenticeship(in DateTime apprenticeDateOfBirth, in DateTime plannedStartDate)
        {
            return apprenticeDateOfBirth.AgeOnThisDay(plannedStartDate);
        }
    }
}
