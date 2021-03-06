using Microsoft.EntityFrameworkCore;
using SFA.DAS.EmployerIncentives.Abstractions.DTOs.Queries;
using SFA.DAS.EmployerIncentives.Data.ApprenticeshipIncentives.Models;
using SFA.DAS.EmployerIncentives.Data.Models;
using SFA.DAS.EmployerIncentives.Domain.Interfaces;
using SFA.DAS.EmployerIncentives.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Data
{
    public class ApprenticeApplicationDataRepository : IApprenticeApplicationDataRepository
    {
        private readonly EmployerIncentivesDbContext _dbContext;
        private readonly IDateTimeService _dateTimeService;
        private readonly ICollectionCalendarService _collectionCalendarService;

        public ApprenticeApplicationDataRepository(
            Lazy<EmployerIncentivesDbContext> dbContext,
            IDateTimeService dateTimeService,
            ICollectionCalendarService collectionCalendarService)
        {
            _dbContext = dbContext.Value;
            _dateTimeService = dateTimeService;
            _collectionCalendarService = collectionCalendarService;
        }

        public async Task<List<ApprenticeApplicationDto>> GetList(long accountId, long accountLegalEntityId)
        {
            var calendar = await _collectionCalendarService.Get();
            var nextActivePeriod = calendar.GetNextPeriod(calendar.GetActivePeriod());

            var accountApplications = from incentive in _dbContext.ApprenticeshipIncentives
                                      from account in _dbContext.Accounts.Where(x => x.AccountLegalEntityId == incentive.AccountLegalEntityId)
                                      from firstPayment in _dbContext.PendingPayments.Where(x => x.ApprenticeshipIncentiveId == incentive.Id && x.EarningType == EarningType.FirstPayment).DefaultIfEmpty()
                                      from secondPayment in _dbContext.PendingPayments.Where(x => x.ApprenticeshipIncentiveId == incentive.Id && x.EarningType == EarningType.SecondPayment).DefaultIfEmpty()
                                      from firstPaymentSent in _dbContext.Payments.Where(x => x.ApprenticeshipIncentiveId == incentive.Id && x.PendingPaymentId == (firstPayment == null ? Guid.Empty : firstPayment.Id)).DefaultIfEmpty()
                                      from learner in _dbContext.Learners.Where(x => x.ApprenticeshipIncentiveId == incentive.Id).DefaultIfEmpty()
                                      where incentive.AccountId == accountId && incentive.AccountLegalEntityId == accountLegalEntityId
                                      select new { incentive, account, firstPayment, secondPayment, learner, firstPaymentSent };

            var result = new List<ApprenticeApplicationDto>();

            foreach (var data in accountApplications)
            {
                var apprenticeApplicationDto = new ApprenticeApplicationDto
                {
                    AccountId = data.incentive.AccountId,
                    AccountLegalEntityId = data.incentive.AccountLegalEntityId,
                    ApplicationDate = data.incentive.SubmittedDate ?? DateTime.Now,
                    FirstName = data.incentive.FirstName,
                    LastName = data.incentive.LastName,
                    ULN = data.incentive.ULN,
                    LegalEntityName = data.account.LegalEntityName,
                    SubmittedByEmail = data.incentive.SubmittedByEmail,
                    TotalIncentiveAmount = data.incentive.PendingPayments.Sum(x => x.Amount),
                    CourseName = data.incentive.CourseName,
                    FirstPaymentStatus = data.firstPayment == default ? null : new PaymentStatusDto
                    {
                        PaymentDate = PaymentDate(data.firstPayment, data.firstPaymentSent, nextActivePeriod),
                        LearnerMatchFound = LearnerMatchFound(data.learner),
                        PaymentAmount = PaymentAmount(data.firstPayment, data.firstPaymentSent),
                        HasDataLock = HasDataLock(data.learner),
                        InLearning = InLearning(data.learner),
                        PausePayments = data.incentive.PausePayments,
                        PaymentSent = data.firstPaymentSent != null,
                        PaymentSentIsEstimated = IsPaymentEstimated(data.firstPaymentSent, _dateTimeService)
                    },
                    SecondPaymentStatus = data.secondPayment == default ? null : new PaymentStatusDto
                    {
                        PaymentDate = data.secondPayment.DueDate.AddMonths(1),
                        LearnerMatchFound = LearnerMatchFound(data.learner),
                        PaymentAmount = data.secondPayment.Amount,
                        HasDataLock = HasDataLock(data.learner),
                        InLearning = InLearning(data.learner),
                        PausePayments = data.incentive.PausePayments,
                        PaymentSentIsEstimated = true // change to use IsPaymentEstimated when implementing ticket EI-827
                    }
                };

                result.Add(apprenticeApplicationDto);

            }

            return result;
        }

        private static bool LearnerMatchFound(Learner learner)
        {
            if(learner == null)
            {
                return false;
            }

            return learner.LearningFound.HasValue && learner.LearningFound.Value;
        }

        private static bool HasDataLock(Learner learner)
        {
            if (learner == null)
            {
                return false;
            }

            return learner.HasDataLock.HasValue && learner.HasDataLock.Value;
        }

        private static bool InLearning(Learner learner)
        {
            if (learner == null)
            {
                return false;
            }

            return learner.InLearning.HasValue && learner.InLearning.Value;
        }
        private static DateTime? PaymentDate(
            PendingPayment pendingPayment, 
            Payment payment,
            Domain.ValueObjects.CollectionPeriod nextActivePeriod)
        {
            if (payment != null)
            {
                if (payment.PaidDate != null)
                {
                    return payment.PaidDate.Value;
                }
                return payment.CalculatedDate;
            }
            
            var activePeriodDate = new DateTime(nextActivePeriod.OpenDate.Year, nextActivePeriod.OpenDate.Month, nextActivePeriod.OpenDate.Day);
            var paymentDueDate = new DateTime(pendingPayment.DueDate.Year, pendingPayment.DueDate.Month, pendingPayment.DueDate.Day);

            if (paymentDueDate < activePeriodDate)
            {
                return new DateTime(nextActivePeriod.CalendarYear, nextActivePeriod.CalendarMonth, 27);
            }
            return pendingPayment.DueDate.AddMonths(1);
        }

        private static decimal? PaymentAmount(PendingPayment pendingPayment, Payment payment)
        {
            if (payment != null)
            {
                return payment.Amount;
            }
            return pendingPayment.Amount;
        }
        
        private static bool IsPaymentEstimated(Payment payment, IDateTimeService dateTimeService)
        {
            if(payment == null || !payment.PaidDate.HasValue)
            {
                return true;
            }

            if (dateTimeService.Now().Day < 27 &&
                payment.PaidDate.Value.Year == dateTimeService.Now().Year &&
                payment.PaidDate.Value.Month == dateTimeService.Now().Month)
            {
                return true;
            }
            return false;
        }

        public async Task<Guid?> GetFirstSubmittedApplicationId(long accountLegalEntityId)
        {
            var firstSubmittedApplicationId = await _dbContext.Applications
                .Where(x => x.AccountLegalEntityId == accountLegalEntityId && x.Status == IncentiveApplicationStatus.Submitted)
                .OrderBy(x => x.DateSubmitted)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            return firstSubmittedApplicationId;
        }
    }
}
