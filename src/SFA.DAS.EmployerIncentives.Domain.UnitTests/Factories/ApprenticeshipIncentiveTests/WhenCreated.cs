﻿using AutoFixture;
using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives.ValueTypes;
using SFA.DAS.EmployerIncentives.Domain.Factories;
using System;

namespace SFA.DAS.EmployerIncentives.Domain.UnitTests.Factories.ApprenticeshipIncentiveTests
{
    public class WhenCreated
    {
        private ApprenticeshipIncentiveFactory _sut;
        private Fixture _fixture;

        [SetUp]
        public void Arrange()
        {
            _sut = new ApprenticeshipIncentiveFactory();
            _fixture = new Fixture();
        }

        [Test]
        public void Then_the_id_is_set()
        {
            // Arrange
            var id = _fixture.Create<Guid>();

            // Act
            var incentive = _sut.CreateNew(id, _fixture.Create<Account>(), _fixture.Create<Apprenticeship>());

            // Assert
            incentive.Id.Should().Be(id);
        }

        [Test]
        public void Then_the_account_is_set()
        {
            // Arrange
            var account = _fixture.Create<Account>();

            // Act
            var incentive = _sut.CreateNew(_fixture.Create<Guid>(), account, _fixture.Create<Apprenticeship>());

            // Assert
            incentive.Account.Id.Should().Be(account.Id);
        }

        [Test]
        public void Then_the_apprenticeship_is_set()
        {
            // Arrange
            var apprenticeship = _fixture.Create<Apprenticeship>();

            // Act
            var incentive = _sut.CreateNew(_fixture.Create<Guid>(), _fixture.Create<Account>(), apprenticeship);

            // Assert
            incentive.Apprenticeship.Id.Should().Be(apprenticeship.Id);
            incentive.Apprenticeship.FirstName.Should().Be(apprenticeship.FirstName);
            incentive.Apprenticeship.LastName.Should().Be(apprenticeship.LastName);
            incentive.Apprenticeship.DateOfBirth.Should().Be(apprenticeship.DateOfBirth);
            incentive.Apprenticeship.UniqueLearnerNumber.Should().Be(apprenticeship.UniqueLearnerNumber);
            incentive.Apprenticeship.EmployerType.Should().Be(apprenticeship.EmployerType);
        }
    }
}