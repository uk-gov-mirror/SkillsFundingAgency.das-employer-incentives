﻿using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerIncentives.Commands.ApprenticeshipIncentive.PaymentProcess;
using SFA.DAS.EmployerIncentives.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Commands.UnitTests.ApprenticeshipIncentive.PaymentProcess
{
    public class WhenHandlingCompleteCommand
    {
        private CompleteCommandHandler _sut;
        private Mock<ICollectionCalendarService> _mockCollectionCalendarService;
        private List<Domain.ValueObjects.CollectionPeriod> _collectionPeriods;
        private Domain.ValueObjects.CollectionPeriod _firstCollectionPeriod;
        private Domain.ValueObjects.CollectionPeriod _secondCollectionPeriod;
        private DateTime _completionDate;

        [SetUp]
        public void Arrange()
        {
            var today = new DateTime(2021, 1, 30);

            _collectionPeriods = new List<Domain.ValueObjects.CollectionPeriod>()
            {
                new Domain.ValueObjects.CollectionPeriod(
                    1,
                    (byte)today.Month,
                    (short)today.Year,
                    today.AddDays(-1),
                    today.AddDays(-1),
                    (short)today.Year,
                    false),
                new Domain.ValueObjects.CollectionPeriod(
                2,
                (byte)today.AddMonths(1).Month,
                (short)today.AddMonths(1).Year,
                today.AddMonths(1).AddDays(-1),
                today.AddMonths(1).AddDays(-1),
                (short)today.AddMonths(1).Year,
                false)
            };
            _firstCollectionPeriod = _collectionPeriods.First();
            _secondCollectionPeriod = _collectionPeriods.First(p => p.PeriodNumber != _firstCollectionPeriod.PeriodNumber);
            _completionDate = DateTime.UtcNow;

            var calendar = new Domain.ValueObjects.CollectionCalendar(_collectionPeriods);

            _mockCollectionCalendarService = new Mock<ICollectionCalendarService>();

            _mockCollectionCalendarService
                .Setup(m => m.Get())
                .ReturnsAsync(calendar);

            _sut = new CompleteCommandHandler(_mockCollectionCalendarService.Object);
        }

        [Test]
        public async Task Then_the_current_period_MonthEndProcessingCompletedDate_is_set()
        {
            //Arrange
            var command = new CompleteCommand(_completionDate, new Domain.ValueObjects.CollectionPeriod(_firstCollectionPeriod.PeriodNumber, _firstCollectionPeriod.CalendarYear));
            var savedPeriods = new List<Domain.ValueObjects.CollectionPeriod>();

            _mockCollectionCalendarService.Setup(m => m.Save(It.IsAny<Domain.ValueObjects.CollectionCalendar>()))
                .Callback<Domain.ValueObjects.CollectionCalendar>(c =>
                {
                    savedPeriods.AddRange(c.GetAllPeriods());
                });

            // Act
            await _sut.Handle(command);

            // Assert
            var savedFirstPeriod = savedPeriods.Single(p => p.PeriodNumber == _firstCollectionPeriod.PeriodNumber);
            savedFirstPeriod.MonthEndProcessingCompletedDate.Should().Be(_completionDate);
        }

        [Test]
        public async Task Then_the_next_period_is_set_to_active()
        {
            //Arrange
            var command = new CompleteCommand(_completionDate, new Domain.ValueObjects.CollectionPeriod(_firstCollectionPeriod.PeriodNumber, _firstCollectionPeriod.CalendarYear));
            var savedPeriods = new List<Domain.ValueObjects.CollectionPeriod>();

            _mockCollectionCalendarService.Setup(m => m.Save(It.IsAny<Domain.ValueObjects.CollectionCalendar>()))
                .Callback<Domain.ValueObjects.CollectionCalendar>(c =>
                {
                    savedPeriods.AddRange(c.GetAllPeriods());
                });

            // Act
            await _sut.Handle(command);

            // Assert
            var savedFirstPeriod = savedPeriods.Single(p => p.PeriodNumber == _firstCollectionPeriod.PeriodNumber);
            savedFirstPeriod.Active.Should().BeFalse();
            var savedSecondPeriod = savedPeriods.Single(p => p.PeriodNumber == _secondCollectionPeriod.PeriodNumber);
            savedSecondPeriod.Active.Should().BeTrue();
            savedSecondPeriod.MonthEndProcessingCompletedDate.Should().BeNull();
        }
    }
}
