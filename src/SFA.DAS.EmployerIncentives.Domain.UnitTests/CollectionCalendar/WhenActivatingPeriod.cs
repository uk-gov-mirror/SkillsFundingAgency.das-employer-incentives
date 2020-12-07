﻿using AutoFixture;
using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.EmployerIncentives.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFA.DAS.EmployerIncentives.Domain.UnitTests.CollectionCalendarTests
{
    [TestFixture]
    public class WhenActivatingPeriod
    {
        private CollectionCalendar _sut;
        private List<CollectionPeriod> _collectionPeriods;
        private Fixture _fixture;
        private DateTime testDate;

        [SetUp]
        public void Arrange()
        {
            _fixture = new Fixture();

            testDate = DateTime.Now;

            var period1 = new CollectionPeriod(1, (byte)testDate.Month, (short)testDate.Year, _fixture.Create<DateTime>(), _fixture.Create<DateTime>(), _fixture.Create<string>(), true);
            var period2 = new CollectionPeriod(2, (byte)testDate.AddMonths(1).Month, (short)testDate.Year, testDate, _fixture.Create<DateTime>(), _fixture.Create<string>(), false);
            var period3 = new CollectionPeriod(3, (byte)testDate.AddMonths(2).Month, (short)testDate.Year, _fixture.Create<DateTime>(), _fixture.Create<DateTime>(), _fixture.Create<string>(), false);

            _collectionPeriods = new List<CollectionPeriod>() { period1, period2, period3 };

            _sut = new CollectionCalendar(_collectionPeriods);
        }

        [Test]
        public void Then_the_initial_period_is_set_to_active()
        {
            // Arrange / Act
            var activePeriod = _sut.GetPeriod((short)testDate.Year, 1);

            // Assert
            activePeriod.Active.Should().BeTrue();
        }

        [Test]
        public void Then_the_active_period_is_changed()
        {
            // Arrange / Act
            _sut.ActivatePeriod((short)testDate.Year, 2, true);

            var periods = _sut.GetAllPeriods().ToList();

            periods.FirstOrDefault(x => x.PeriodNumber == 1).Active.Should().BeFalse();
            periods.FirstOrDefault(x => x.PeriodNumber == 2).Active.Should().BeTrue();
            periods.FirstOrDefault(x => x.PeriodNumber == 3).Active.Should().BeFalse();
            periods.Count(x => x.Active == true).Should().Be(1);
        }

        [Test]
        public void Then_the_active_period_is_not_changed_when_the_period_and_year_not_matched()
        {
            // Arrange / Act
            _sut.ActivatePeriod((short)testDate.Year, 4, true);

            var periods = _sut.GetAllPeriods().ToList();

            periods.FirstOrDefault(x => x.PeriodNumber == 1).Active.Should().BeTrue();
            periods.FirstOrDefault(x => x.PeriodNumber == 2).Active.Should().BeFalse();
            periods.FirstOrDefault(x => x.PeriodNumber == 3).Active.Should().BeFalse();
            periods.Count(x => x.Active == true).Should().Be(1);
        }

        [Test]
        public void Then_the_active_period_is_set_to_inactive()
        {
            // Arrange / Act
            _sut.ActivatePeriod((short)testDate.Year, 1, false);

            var periods = _sut.GetAllPeriods().ToList();

            periods.FirstOrDefault(x => x.PeriodNumber == 1).Active.Should().BeFalse();
            periods.FirstOrDefault(x => x.PeriodNumber == 2).Active.Should().BeFalse();
            periods.FirstOrDefault(x => x.PeriodNumber == 3).Active.Should().BeFalse();
            periods.Count(x => x.Active == true).Should().Be(0);
        }

        [Test]
        public void Then_setting_one_period_to_inactive_does_not_deactivate_other_periods()
        {
            // Arrange / Act
            _sut.ActivatePeriod((short)testDate.Year, 2, false);

            var periods = _sut.GetAllPeriods().ToList();

            periods.FirstOrDefault(x => x.PeriodNumber == 1).Active.Should().BeTrue();
            periods.FirstOrDefault(x => x.PeriodNumber == 2).Active.Should().BeFalse();
            periods.FirstOrDefault(x => x.PeriodNumber == 3).Active.Should().BeFalse();
            periods.Count(x => x.Active == true).Should().Be(1);
        }
    }
}
