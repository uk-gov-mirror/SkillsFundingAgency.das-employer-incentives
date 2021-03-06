using AutoFixture;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives.Events;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives.Models;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives.ValueTypes;
using SFA.DAS.EmployerIncentives.Domain.Factories;
using SFA.DAS.EmployerIncentives.Domain.Interfaces;
using SFA.DAS.EmployerIncentives.Domain.ValueObjects;
using SFA.DAS.EmployerIncentives.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Domain.UnitTests.ApprenticeshipIncentiveTests
{
    [TestFixture]
    public class WhenCalculateEarnings
    {
        private ApprenticeshipIncentive _sut;
        private ApprenticeshipIncentiveModel _sutModel;
        private Mock<ICollectionCalendarService> _mockCollectionCalendarService;
        private Mock<IIncentivePaymentProfilesService> _mockPaymentProfilesService;
        private Fixture _fixture;

        private List<IncentivePaymentProfile> _paymentProfiles;
        private List<CollectionPeriod> _collectionPeriods;
        private CollectionCalendar _collectionCalendar;
        private Apprenticeship _apprenticehip;
        private int _firstPaymentDaysAfterApprenticeshipStart;
        private int _secondPaymentDaysAfterApprenticeshipStart;
        private DateTime _collectionPeriod;
        private DateTime _plannedStartDate;

        [SetUp]
        public void Arrange()
        {
            _fixture = new Fixture();

            _collectionPeriod = new DateTime(2020, 10, 1);
            _plannedStartDate = _collectionPeriod.AddDays(5);

            _firstPaymentDaysAfterApprenticeshipStart = 10;
            _secondPaymentDaysAfterApprenticeshipStart = 50;

            var under25PaymentProfiles = new List<PaymentProfile>
            {
                new PaymentProfile(_firstPaymentDaysAfterApprenticeshipStart, 100),
                new PaymentProfile(_secondPaymentDaysAfterApprenticeshipStart, 300)
            };

            var over25PaymentProfiles = new List<PaymentProfile>
            {
                new PaymentProfile(_firstPaymentDaysAfterApprenticeshipStart, 200),
                new PaymentProfile(_secondPaymentDaysAfterApprenticeshipStart, 400)
            };

            _paymentProfiles = new List<IncentivePaymentProfile>()
            {
                new IncentivePaymentProfile(Enums.IncentiveType.UnderTwentyFiveIncentive, under25PaymentProfiles),
                new IncentivePaymentProfile(IncentiveType.TwentyFiveOrOverIncentive, over25PaymentProfiles)
            };

            _collectionPeriods = new List<CollectionPeriod>()
            {
                new CollectionPeriod(1, (byte)_collectionPeriod.AddMonths(-1).Month, (short)_collectionPeriod.AddMonths(-1).Year, _collectionPeriod.AddMonths(-1).AddDays(1), _fixture.Create<DateTime>(), _fixture.Create<short>(), false),
                new CollectionPeriod(2, (byte)_collectionPeriod.AddMonths(1).Month, (short)_collectionPeriod.AddMonths(1).Year, _collectionPeriod.AddMonths(1).AddDays(1), _fixture.Create<DateTime>(), _fixture.Create<short>(), false),
                new CollectionPeriod(3, (byte)_collectionPeriod.AddMonths(2).Month, (short)_collectionPeriod.AddMonths(2).Year, _collectionPeriod.AddMonths(2).AddDays(1), _fixture.Create<DateTime>(), _fixture.Create<short>(), false)
            };

            _collectionCalendar = new CollectionCalendar(_collectionPeriods);

            _mockCollectionCalendarService = new Mock<ICollectionCalendarService>();
            _mockCollectionCalendarService.Setup(m => m.Get()).ReturnsAsync(_collectionCalendar);

            _mockPaymentProfilesService = new Mock<IIncentivePaymentProfilesService>();
            _mockPaymentProfilesService.Setup(m => m.Get()).ReturnsAsync(_paymentProfiles);

            _sutModel = _fixture.Create<ApprenticeshipIncentiveModel>();
            _apprenticehip = _sutModel.Apprenticeship;
            _sutModel.StartDate = _plannedStartDate;
            _sutModel.PendingPaymentModels = new List<PendingPaymentModel>();
            _sutModel.PaymentModels = new List<PaymentModel>();
            _sutModel.ClawbackPaymentModels = new List<ClawbackPaymentModel>();
            _sut = Sut(_sutModel);
        }

        [Test]
        public async Task Then_earnings_are_not_calculated_when_the_incentive_does_not_pass_the_eligibility_check()
        {
            // arrange            
            var apprentiveshipDob = DateTime.Now.AddYears(-24);
            _sutModel.StartDate = Incentive.EligibilityStartDate.AddDays(-1);
            _sutModel.Apprenticeship = new Apprenticeship(_apprenticehip.Id, _apprenticehip.FirstName, _apprenticehip.LastName, apprentiveshipDob, _apprenticehip.UniqueLearnerNumber, _apprenticehip.EmployerType, _apprenticehip.CourseName);

            _collectionPeriods.Add(new CollectionPeriod(4, (byte)_collectionPeriod.AddMonths(3).Month, (short)_collectionPeriod.AddMonths(3).Year, _collectionPeriod.AddMonths(3).AddDays(1), _fixture.Create<DateTime>(), _fixture.Create<short>(), true));

            // act
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);

            // assert
            _sut.PendingPayments.Should().BeEmpty();
        }

        [Test]
        public async Task Then_the_earnings_are_calculated_and_the_pending_payments_created_using_the_planned_start_date_when_no_actual_start_date()
        {
            // arrange                        

            // act
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);

            // assert
            _sut.PendingPayments.Count.Should().Be(2);

            var firstPayment = _sut.PendingPayments.First();
            var secondPayment = _sut.PendingPayments.Last();

            firstPayment.DueDate.Should().Be(_sutModel.StartDate.AddDays(_firstPaymentDaysAfterApprenticeshipStart));
            secondPayment.DueDate.Should().Be(_sutModel.StartDate.AddDays(_secondPaymentDaysAfterApprenticeshipStart));

            firstPayment.PeriodNumber.Should().Be(1);
            firstPayment.PaymentYear.Should().Be(_collectionPeriods.Single(x => x.PeriodNumber == 1).AcademicYear);
            firstPayment.Amount.Should().Be(100);
            secondPayment.PeriodNumber.Should().Be(2);
            secondPayment.PaymentYear.Should().Be(_collectionPeriods.Single(x => x.PeriodNumber == 2).AcademicYear);
            secondPayment.Amount.Should().Be(300);

            firstPayment.Account.Id.Should().Be(_sutModel.Account.Id);
            firstPayment.Account.AccountLegalEntityId.Should().Be(_sutModel.Account.AccountLegalEntityId);
            secondPayment.Account.Id.Should().Be(_sutModel.Account.Id);
            secondPayment.Account.AccountLegalEntityId.Should().Be(_sutModel.Account.AccountLegalEntityId);

            firstPayment.EarningType.Should().Be(EarningType.FirstPayment);
            secondPayment.EarningType.Should().Be(EarningType.SecondPayment);
        }

        [Test]
        public async Task Then_the_earnings_are_calculated_and_the_pending_payments_created_using_the_actual_start_date_when_there_is_an_actual_start_date()
        {
            // arrange           
            _sutModel.StartDate = _collectionPeriod.Date.AddDays(6);

            // act
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);

            // assert
            _sut.PendingPayments.Count.Should().Be(2);

            var firstPayment = _sut.PendingPayments.First();
            var secondPayment = _sut.PendingPayments.Last();

            firstPayment.DueDate.Should().Be(_sutModel.StartDate.AddDays(_firstPaymentDaysAfterApprenticeshipStart));
            secondPayment.DueDate.Should().Be(_sutModel.StartDate.AddDays(_secondPaymentDaysAfterApprenticeshipStart));

            firstPayment.PeriodNumber.Should().Be(1);
            firstPayment.PaymentYear.Should().Be(_collectionPeriods.Single(x => x.PeriodNumber == 1).AcademicYear);
            firstPayment.Amount.Should().Be(100);
            secondPayment.PeriodNumber.Should().Be(2);
            secondPayment.PaymentYear.Should().Be(_collectionPeriods.Single(x => x.PeriodNumber == 2).AcademicYear);
            secondPayment.Amount.Should().Be(300);

            firstPayment.Account.Id.Should().Be(_sutModel.Account.Id);
            firstPayment.Account.AccountLegalEntityId.Should().Be(_sutModel.Account.AccountLegalEntityId);
            secondPayment.Account.Id.Should().Be(_sutModel.Account.Id);
            secondPayment.Account.AccountLegalEntityId.Should().Be(_sutModel.Account.AccountLegalEntityId);
        }

        [Test]
        public async Task Then_an_EarningsCalculated_event_is_raised_after_the_earnings_are_calculated()
        {
            // arrange                        
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);

            // act
            var events = _sut.FlushEvents();

            // assert
            var expectedEvent = events.Single() as EarningsCalculated;

            expectedEvent.ApprenticeshipIncentiveId.Should().Be(_sutModel.Id);
            expectedEvent.AccountId.Should().Be(_sutModel.Account.Id);
            expectedEvent.ApprenticeshipId.Should().Be(_sutModel.Apprenticeship.Id);
            expectedEvent.ApplicationApprenticeshipId.Should().Be(_sutModel.ApplicationApprenticeshipId);
        }

        [Test]
        public async Task Then_RefreshedLearnerForEarnings_is_set_to_false_when_earnings_are_calculated()
        {
            // arrange
            _sutModel.RefreshedLearnerForEarnings = true;

            // act
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);

            // assert
            _sut.RefreshedLearnerForEarnings.Should().BeFalse();
        }

        [Test]
        public async Task Then_earnings_with_sent_payments_are_clawed_back_when_the_collection_period_has_changed()
        {
            // arrange
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);
            byte collectionPeriod = 6;
            short collectionYear = 2020;
            var pendingPayment = _sutModel.PendingPaymentModels.Single(x => x.EarningType == EarningType.FirstPayment);
            pendingPayment.PendingPaymentValidationResultModels = new List<PendingPaymentValidationResultModel>();
            pendingPayment.PendingPaymentValidationResultModels.Add(_fixture.Build<PendingPaymentValidationResultModel>().With(x => x.CollectionPeriod, new CollectionPeriod(collectionPeriod, collectionYear)).With(x => x.Result, true).Create());
            _sut.CreatePayment(pendingPayment.Id, collectionPeriod, collectionPeriod);
            _sutModel.PaymentModels.First().PaidDate = DateTime.Now;

            _collectionPeriods.Add(new CollectionPeriod(4, (byte)_collectionPeriod.AddMonths(3).Month, (short)_collectionPeriod.AddMonths(3).Year, _collectionPeriod.AddMonths(3).AddDays(1), _fixture.Create<DateTime>(), _fixture.Create<short>(), true));

            // act
            _sut.SetStartDate(_plannedStartDate.AddMonths(1));
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);

            // assert
            pendingPayment.ClawedBack.Should().BeTrue();
            _sutModel.PendingPaymentModels.Count(x => x.EarningType == EarningType.FirstPayment).Should().Be(2);
        }

        [Test]
        public async Task Then_clawback_payment_is_created_when_earnings_with_sent_payments_are_clawed_back_and_the_collection_period_is_changed()
        {
            // arrange
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);
            byte collectionPeriod = 6;
            short collectionYear = 2020;
            var pendingPayment = _sutModel.PendingPaymentModels.Single(x => x.EarningType == EarningType.FirstPayment);
            pendingPayment.PendingPaymentValidationResultModels = new List<PendingPaymentValidationResultModel>();
            pendingPayment.PendingPaymentValidationResultModels.Add(_fixture.Build<PendingPaymentValidationResultModel>().With(x => x.CollectionPeriod, new CollectionPeriod(collectionPeriod, collectionYear)).With(x => x.Result, true).Create());
            _sut.CreatePayment(pendingPayment.Id, collectionPeriod, collectionPeriod);
            _sutModel.PaymentModels.First().PaidDate = DateTime.Now;

            var activePeriod = new CollectionPeriod(4, (byte)_collectionPeriod.AddMonths(3).Month, (short)_collectionPeriod.AddMonths(3).Year, _collectionPeriod.AddMonths(3).AddDays(1), _fixture.Create<DateTime>(), _fixture.Create<short>(), true);
            _collectionPeriods.Add(activePeriod);

            // act
            _sut.SetStartDate(_plannedStartDate.AddMonths(1));
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);

            // assert
            var clawback = _sutModel.ClawbackPaymentModels.Single();
            clawback.ApprenticeshipIncentiveId.Should().Be(_sutModel.Id);
            clawback.PendingPaymentId.Should().Be(pendingPayment.Id);
            clawback.PaymentId.Should().Be(_sutModel.PaymentModels.First().Id);
            clawback.Account.Should().Be(_sutModel.Account);
            clawback.Amount.Should().Be(-1 * pendingPayment.Amount);
            clawback.SubnominalCode.Should().Be(_sutModel.PaymentModels.First().SubnominalCode);
            clawback.CollectionPeriod.Should().Be(activePeriod.PeriodNumber);
            clawback.CollectionPeriodYear.Should().Be(activePeriod.AcademicYear);
        }

        [Test]
        public async Task Then_clawback_payment_is_not_created_when_one_already_exists_and_earnings_with_sent_payments_are_clawed_back_and_the_collection_period_is_changed()
        {
            // arrange
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);
            byte collectionPeriod = 6;
            short collectionYear = 2020;
            var pendingPayment = _sutModel.PendingPaymentModels.Single(x => x.EarningType == EarningType.FirstPayment);
            pendingPayment.PendingPaymentValidationResultModels = new List<PendingPaymentValidationResultModel>();
            pendingPayment.PendingPaymentValidationResultModels.Add(_fixture.Build<PendingPaymentValidationResultModel>().With(x => x.CollectionPeriod, new CollectionPeriod(collectionPeriod, collectionYear)).With(x => x.Result, true).Create());
            _sut.CreatePayment(pendingPayment.Id, collectionPeriod, collectionPeriod);
            _sutModel.PaymentModels.First().PaidDate = DateTime.Now;
            _sutModel.ClawbackPaymentModels.Add(
                _fixture.Build<ClawbackPaymentModel>()
                .With(x => x.ApprenticeshipIncentiveId, _sutModel.ApplicationApprenticeshipId)
                .With(x => x.PendingPaymentId, pendingPayment.Id)
                .Create());

            _collectionPeriods.Add(new CollectionPeriod(4, (byte)_collectionPeriod.AddMonths(3).Month, (short)_collectionPeriod.AddMonths(3).Year, _collectionPeriod.AddMonths(3).AddDays(1), _fixture.Create<DateTime>(), _fixture.Create<short>(), true));

            // act
            _sut.SetStartDate(_plannedStartDate.AddMonths(1));
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);

            // assert
            _sutModel.ClawbackPaymentModels.Count.Should().Be(1);
        }

        [Test]
        public async Task Then_earnings_with_sent_payments_are_clawed_back_when_the_earning_amount_has_changed()
        {
            // arrange
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);
            byte collectionPeriod = 6;
            short collectionYear = 2020;
            var pendingPayment = _sutModel.PendingPaymentModels.Single(x => x.EarningType == EarningType.FirstPayment);
            pendingPayment.PendingPaymentValidationResultModels = new List<PendingPaymentValidationResultModel>();
            pendingPayment.PendingPaymentValidationResultModels.Add(_fixture.Build<PendingPaymentValidationResultModel>().With(x => x.CollectionPeriod, new CollectionPeriod(collectionPeriod, collectionYear)).With(x => x.Result, true).Create());
            _sut.CreatePayment(pendingPayment.Id, collectionPeriod, collectionPeriod);
            _sutModel.PaymentModels.First().PaidDate = DateTime.Now;

            _collectionPeriods.Add(new CollectionPeriod(4, (byte)_collectionPeriod.AddMonths(3).Month, (short)_collectionPeriod.AddMonths(3).Year, _collectionPeriod.AddMonths(3).AddDays(1), _fixture.Create<DateTime>(), _fixture.Create<short>(), true));

            // act
            var apprenticeshipDob = DateTime.Now.AddYears(-26);
            _sutModel.Apprenticeship = new Apprenticeship(_apprenticehip.Id, _apprenticehip.FirstName, _apprenticehip.LastName, apprenticeshipDob, _apprenticehip.UniqueLearnerNumber, _apprenticehip.EmployerType, _apprenticehip.CourseName);
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);

            // assert
            pendingPayment.ClawedBack.Should().BeTrue();
            _sutModel.PendingPaymentModels.Count(x => x.EarningType == EarningType.FirstPayment).Should().Be(2);
        }

        [Test]
        public async Task Then_clawback_payment_is_created_when_earnings_with_sent_payments_are_clawed_back_and_the_earning_amount_has_changed()
        {
            // arrange
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);
            byte collectionPeriod = 6;
            short collectionYear = 2020;
            var pendingPayment = _sutModel.PendingPaymentModels.Single(x => x.EarningType == EarningType.FirstPayment);
            pendingPayment.PendingPaymentValidationResultModels = new List<PendingPaymentValidationResultModel>();
            pendingPayment.PendingPaymentValidationResultModels.Add(_fixture.Build<PendingPaymentValidationResultModel>().With(x => x.CollectionPeriod, new CollectionPeriod(collectionPeriod, collectionYear)).With(x => x.Result, true).Create());
            _sut.CreatePayment(pendingPayment.Id, collectionPeriod, collectionPeriod);
            _sutModel.PaymentModels.First().PaidDate = DateTime.Now;

            var activePeriod = new CollectionPeriod(4, (byte)_collectionPeriod.AddMonths(3).Month, (short)_collectionPeriod.AddMonths(3).Year, _collectionPeriod.AddMonths(3).AddDays(1), _fixture.Create<DateTime>(), _fixture.Create<short>(), true);
            _collectionPeriods.Add(activePeriod);

            // act
            var apprenticeshipDob = DateTime.Now.AddYears(-26);
            _sutModel.Apprenticeship = new Apprenticeship(_apprenticehip.Id, _apprenticehip.FirstName, _apprenticehip.LastName, apprenticeshipDob, _apprenticehip.UniqueLearnerNumber, _apprenticehip.EmployerType, _apprenticehip.CourseName);
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);

            // assert
            var clawback = _sutModel.ClawbackPaymentModels.Single();
            clawback.ApprenticeshipIncentiveId.Should().Be(_sutModel.Id);
            clawback.PendingPaymentId.Should().Be(pendingPayment.Id);
            clawback.PaymentId.Should().Be(_sutModel.PaymentModels.First().Id);
            clawback.Account.Should().Be(_sutModel.Account);
            clawback.Amount.Should().Be(-1 * pendingPayment.Amount);
            clawback.SubnominalCode.Should().Be(_sutModel.PaymentModels.First().SubnominalCode);
            clawback.CollectionPeriod.Should().Be(activePeriod.PeriodNumber);
            clawback.CollectionPeriodYear.Should().Be(activePeriod.AcademicYear);
        }

        [Test]
        public async Task Then_paid_earnings_are_clawed_back_if_the_apprenticeship_is_no_longer_eligible()
        {
            // arrange
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);
            byte collectionPeriod = 6;
            short collectionYear = 2020;
            var pendingPayment = _sutModel.PendingPaymentModels.Single(x => x.EarningType == EarningType.FirstPayment);
            pendingPayment.PendingPaymentValidationResultModels = new List<PendingPaymentValidationResultModel>();
            pendingPayment.PendingPaymentValidationResultModels.Add(_fixture.Build<PendingPaymentValidationResultModel>().With(x => x.CollectionPeriod, new CollectionPeriod(collectionPeriod, collectionYear)).With(x => x.Result, true).Create());
            _sut.CreatePayment(pendingPayment.Id, collectionPeriod, collectionPeriod);
            _sutModel.PaymentModels.First().PaidDate = DateTime.Now;

            _collectionPeriods.Add(new CollectionPeriod(4, (byte)_collectionPeriod.AddMonths(3).Month, (short)_collectionPeriod.AddMonths(3).Year, _collectionPeriod.AddMonths(3).AddDays(1), _fixture.Create<DateTime>(), _fixture.Create<short>(), true));

            // act
            _sutModel.StartDate = Incentive.EligibilityStartDate.AddDays(-1);
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);

            // assert
            pendingPayment.ClawedBack.Should().BeTrue();
            _sutModel.PendingPaymentModels.Count.Should().Be(1);
        }

        [Test]
        public async Task Then_clawback_payment_is_created_when_paid_earnings_are_clawed_back_if_the_apprenticeship_is_no_longer_eligible()
        {
            // arrange
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);
            byte collectionPeriod = 6;
            short collectionYear = 2020;
            var pendingPayment = _sutModel.PendingPaymentModels.Single(x => x.EarningType == EarningType.FirstPayment);
            pendingPayment.PendingPaymentValidationResultModels = new List<PendingPaymentValidationResultModel>();
            pendingPayment.PendingPaymentValidationResultModels.Add(_fixture.Build<PendingPaymentValidationResultModel>().With(x => x.CollectionPeriod, new CollectionPeriod(collectionPeriod, collectionYear)).With(x => x.Result, true).Create());
            _sut.CreatePayment(pendingPayment.Id, collectionPeriod, collectionPeriod);
            _sutModel.PaymentModels.First().PaidDate = DateTime.Now;

            var activePeriod = new CollectionPeriod(4, (byte)_collectionPeriod.AddMonths(3).Month, (short)_collectionPeriod.AddMonths(3).Year, _collectionPeriod.AddMonths(3).AddDays(1), _fixture.Create<DateTime>(), _fixture.Create<short>(), true);
            _collectionPeriods.Add(activePeriod);

            // act
            _sutModel.StartDate = Incentive.EligibilityStartDate.AddDays(-1);
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);

            // assert
            var clawback = _sutModel.ClawbackPaymentModels.Single();
            clawback.ApprenticeshipIncentiveId.Should().Be(_sutModel.Id);
            clawback.PendingPaymentId.Should().Be(pendingPayment.Id);
            clawback.PaymentId.Should().Be(_sutModel.PaymentModels.First().Id);
            clawback.Account.Should().Be(_sutModel.Account);
            clawback.Amount.Should().Be(-1 * pendingPayment.Amount);
            clawback.SubnominalCode.Should().Be(_sutModel.PaymentModels.First().SubnominalCode);
            clawback.CollectionPeriod.Should().Be(activePeriod.PeriodNumber);
            clawback.CollectionPeriodYear.Should().Be(activePeriod.AcademicYear);
        }

        [Test]
        public async Task Then_paid_earnings_are_not_clawed_back_when_the_new_earning_is_in_the_same_collection_period()
        {
            // arrange
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);
            byte collectionPeriod = 6;
            short collectionYear = 2020;
            var pendingPayment = _sutModel.PendingPaymentModels.Single(x => x.EarningType == EarningType.FirstPayment);
            pendingPayment.PendingPaymentValidationResultModels = new List<PendingPaymentValidationResultModel>();
            pendingPayment.PendingPaymentValidationResultModels.Add(_fixture.Build<PendingPaymentValidationResultModel>().With(x => x.CollectionPeriod, new CollectionPeriod(collectionPeriod, collectionYear)).With(x => x.Result, true).Create());
            _sut.CreatePayment(pendingPayment.Id, collectionPeriod, collectionPeriod);
            _sutModel.PaymentModels.First().PaidDate = DateTime.Now;

            // act
            _sut.SetStartDate(_plannedStartDate.AddDays(1));
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);

            // assert
            pendingPayment.ClawedBack.Should().BeFalse();
            _sutModel.PendingPaymentModels.Count(x => x.EarningType == EarningType.FirstPayment).Should().Be(1);
            _sutModel.ClawbackPaymentModels.Count.Should().Be(0);
        }

        [Test]
        public async Task Then_earnings_with_unsent_payments_are_removed_when_the_earnings_change()
        {
            // Arrange
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);
            _sutModel.PaymentModels = new List<PaymentModel>();
            _sutModel.PendingPaymentModels.ToList().ForEach(p => _sutModel.PaymentModels.Add(_fixture.Build<PaymentModel>().With(x => x.PendingPaymentId, p.Id).With(x => x.PaidDate, (DateTime?)null).Create()));
            var hashCodes = new List<int>();
            _sut.PendingPayments.ToList().ForEach(p => hashCodes.Add(p.GetHashCode()));

            // Act
            _sut.SetStartDate(_plannedStartDate.AddMonths(1));
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);

            // Assert
            _sut.PendingPayments.Count.Should().Be(2);
            _sut.PendingPayments.ToList().ForEach(p => hashCodes.Contains(p.GetHashCode()).Should().BeFalse());
            _sut.Payments.Should().BeEmpty();
        }

        [Test]
        public async Task Then_earnings_without_payments_are_removed_when_the_earnings_change()
        {
            // Arrange
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);
            var hashCodes = new List<int>();
            _sut.PendingPayments.ToList().ForEach(p => hashCodes.Add(p.GetHashCode()));

            // Act
            _sut.SetStartDate(_plannedStartDate.AddMonths(1));
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);

            // Assert
            _sut.PendingPayments.Count.Should().Be(2);
            _sut.PendingPayments.ToList().ForEach(p => hashCodes.Contains(p.GetHashCode()).Should().BeFalse());
        }

        [Test]
        public async Task Then_existing_earnings_are_kept_when_the_earnings_are_unchanged()
        {
            // Arrange
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);
            var hashCodes = new List<int>();
            _sut.PendingPayments.ToList().ForEach(p => hashCodes.Add(p.GetHashCode()));

            // Act
            await _sut.CalculateEarnings(_mockPaymentProfilesService.Object, _mockCollectionCalendarService.Object);

            // Assert
            _sut.PendingPayments.Count.Should().Be(2);
            _sut.PendingPayments.ToList().ForEach(p => hashCodes.Contains(p.GetHashCode()).Should().BeTrue());
        }


        private ApprenticeshipIncentive Sut(ApprenticeshipIncentiveModel model)
        {
            return ApprenticeshipIncentive.Get(model.Id, model);
        }

        [Test]
        public void Then_there_are_no_pending_payments()
        {
            // Arrange            

            // Act
            var incentive = new ApprenticeshipIncentiveFactory().CreateNew(
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>(),
                _fixture.Create<ApprenticeshipIncentives.ValueTypes.Account>(),
                _fixture.Create<ApprenticeshipIncentives.ValueTypes.Apprenticeship>(),
                _fixture.Create<DateTime>(),
                _fixture.Create<DateTime>(),
                _fixture.Create<string>());

            // Assert
            incentive.PendingPayments.Count.Should().Be(0);
            incentive.GetModel().PendingPaymentModels.Count.Should().Be(0);
        }
    }
}
