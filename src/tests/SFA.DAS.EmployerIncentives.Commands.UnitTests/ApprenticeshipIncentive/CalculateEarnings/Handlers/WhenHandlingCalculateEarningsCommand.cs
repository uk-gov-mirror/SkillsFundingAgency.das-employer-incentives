using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Common.Domain.Types;
using SFA.DAS.EmployerIncentives.Commands.ApprenticeshipIncentive.CalculateEarnings;
using SFA.DAS.EmployerIncentives.Commands.Persistence;
using SFA.DAS.EmployerIncentives.Commands.Services;
using SFA.DAS.EmployerIncentives.Commands.Types.ApprenticeshipIncentive;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives.Events;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives.ValueTypes;
using SFA.DAS.EmployerIncentives.Domain.Factories;
using SFA.DAS.EmployerIncentives.Domain.Interfaces;
using SFA.DAS.EmployerIncentives.Domain.ValueObjects;
using SFA.DAS.EmployerIncentives.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Commands.UnitTests.ApprenticeshipIncentive.CalculateEarnings.Handlers
{
    public class WhenHandlingCreateCommand
    {
        private CalculateEarningsCommandHandler _sut;
        private Mock<IIncentivePaymentProfilesService> _mockPaymentProfilesService;
        private Mock<IApprenticeshipIncentiveDomainRepository> _mockIncentiveDomainRespository;
        private Mock<ICollectionCalendarService> _mockCollectionCalendarService;
        private Fixture _fixture;
        private List<IncentivePaymentProfile> _paymentProfiles;
        private List<Domain.ValueObjects.CollectionPeriod> _collectionPeriods;

        [SetUp]
        public void Arrange()
        {
            var today = new DateTime(2021, 1, 30);

            _fixture = new Fixture();

            _mockPaymentProfilesService = new Mock<IIncentivePaymentProfilesService>();
            _mockIncentiveDomainRespository = new Mock<IApprenticeshipIncentiveDomainRepository>();
            _mockCollectionCalendarService = new Mock<ICollectionCalendarService>();

            _paymentProfiles = new List<IncentivePaymentProfile>
            {
                new IncentivePaymentProfile(
                    IncentiveType.TwentyFiveOrOverIncentive, new List<PaymentProfile>
                    {
                        new PaymentProfile(10, 100),
                        new PaymentProfile(100, 1000)
                    })
            };

            _mockPaymentProfilesService
               .Setup(m => m.Get())
               .ReturnsAsync(_paymentProfiles);

            _collectionPeriods = new List<Domain.ValueObjects.CollectionPeriod>()
            {
                new Domain.ValueObjects.CollectionPeriod(1, (byte)today.Month, (short)today.Year, today.AddDays(-1), _fixture.Create<DateTime>(), _fixture.Create<short>(), false)
            };

            _mockCollectionCalendarService
                .Setup(m => m.Get())
                .ReturnsAsync(new Domain.ValueObjects.CollectionCalendar(_collectionPeriods));

            var incentive = new ApprenticeshipIncentiveFactory()
                    .CreateNew(_fixture.Create<Guid>(),
                    _fixture.Create<Guid>(),
                    _fixture.Create<Account>(),
                    new Apprenticeship(
                        _fixture.Create<long>(),
                        _fixture.Create<string>(),
                        _fixture.Create<string>(),
                        today.AddYears(-26),
                        _fixture.Create<long>(),
                        ApprenticeshipEmployerType.Levy,
                        _fixture.Create<string>()
                        ),
                    today,
                    _fixture.Create<DateTime>(),
                    _fixture.Create<string>());
            
            incentive.Apprenticeship.SetProvider(_fixture.Create<Provider>());

            _fixture.Register(() => incentive);

            _sut = new CalculateEarningsCommandHandler(
                _mockIncentiveDomainRespository.Object,
                _mockPaymentProfilesService.Object,
                _mockCollectionCalendarService.Object);
        }

        [Test]
        public async Task Then_a_earnings_calculated_event_is_raised()
        {
            //Arrange
            var incentive = _fixture.Create<Domain.ApprenticeshipIncentives.ApprenticeshipIncentive>();

            var command = new CalculateEarningsCommand(incentive.Id);

            _mockIncentiveDomainRespository.Setup(x => x
            .Find(command.ApprenticeshipIncentiveId))
                .ReturnsAsync(incentive);

            // Act
            await _sut.Handle(command);

            // Assert
            incentive.FlushEvents().OfType<EarningsCalculated>().ToList().Count.Should().Be(1);
        }

        [Test]
        public async Task Then_a_pending_payment_is_created_for_each_payment_profile()
        {
            //Arrange
            var incentive = _fixture.Create<Domain.ApprenticeshipIncentives.ApprenticeshipIncentive>();

            var command = new CalculateEarningsCommand(incentive.Id);

            _mockIncentiveDomainRespository.Setup(x => x
            .Find(command.ApprenticeshipIncentiveId))
                .ReturnsAsync(incentive);

            // Act
            await _sut.Handle(command);

            // Assert
            incentive.PendingPayments.Count.Should().Be(2);
        }

        [Test]
        public async Task Then_the_earnings_calculated_is_persisted()
        {
            //Arrange
            var incentive = _fixture.Create<Domain.ApprenticeshipIncentives.ApprenticeshipIncentive>();

            var command = new CalculateEarningsCommand(incentive.Id);

            _mockIncentiveDomainRespository.Setup(x => x
            .Find(command.ApprenticeshipIncentiveId))
                .ReturnsAsync(incentive);

            int itemsPersisted = 0;
            _mockIncentiveDomainRespository.Setup(m => m.Save(It.Is<Domain.ApprenticeshipIncentives.ApprenticeshipIncentive>( a => a.Id == command.ApprenticeshipIncentiveId)))
                                            .Callback(() =>
                                            {
                                                itemsPersisted++;
                                            });


            // Act
            await _sut.Handle(command);

            // Assert
            itemsPersisted.Should().Be(1);
        }
    }
}
