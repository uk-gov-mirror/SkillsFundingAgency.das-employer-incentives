﻿using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SFA.DAS.EmployerIncentives.Data.Models;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Data.UnitTests.ApprenticeshipIncentive
{
    public class WhenUpdateCalled
    {
        private ApprenticeshipIncentives.ApprenticeshipIncentiveDataRepository _sut;
        private Fixture _fixture;
        private EmployerIncentivesDbContext _dbContext;
        private ApprenticeshipIncentiveModel _testIncentive;

        [SetUp]
        public async Task Arrange()
        {
            _fixture = new Fixture();

            var options = new DbContextOptionsBuilder<EmployerIncentivesDbContext>()
                .UseInMemoryDatabase("EmployerIncentivesDbContext" + Guid.NewGuid()).Options;
            _dbContext = new EmployerIncentivesDbContext(options);

            _testIncentive = _fixture
                .Build<ApprenticeshipIncentiveModel>()
                .Create();

            _sut = new ApprenticeshipIncentives.ApprenticeshipIncentiveDataRepository(_dbContext);
            await _sut.Add(_testIncentive);
        }

        [TearDown]
        public void CleanUp()
        {
            _dbContext.Dispose();
        }

        [Test]
        public async Task Then_the_apprenticeship_incentive_is_updated_with_new_values()
        {
            // Arrange
            var storedIncentive = await _sut.Get(_testIncentive.Id);

            storedIncentive.Account = _fixture.Create<Domain.ApprenticeshipIncentives.ValueTypes.Account>();
            storedIncentive.Apprenticeship = _fixture.Create<Domain.ApprenticeshipIncentives.ValueTypes.Apprenticeship>();

            var pendingPayments = _fixture.Build<PendingPaymentModel>().With(
                x => x.ApprenticeshipIncentiveId, storedIncentive.Id).CreateMany().ToList();
            storedIncentive.PendingPaymentModels = pendingPayments;

            var validationResults = _fixture.CreateMany<PendingPaymentValidationResultModel>().ToList();
            storedIncentive.PendingPaymentModels.First().PendingPaymentValidationResultModels = validationResults;

            // Act
            await _sut.Update(storedIncentive);

            // Assert
            _dbContext.ApprenticeshipIncentives.Count().Should().Be(1);
            _dbContext.ApprenticeshipIncentives.Count(
                x => x.AccountId == storedIncentive.Account.Id &&
                     x.ApprenticeshipId == storedIncentive.Apprenticeship.Id &&
                     x.FirstName == storedIncentive.Apprenticeship.FirstName &&
                     x.LastName == storedIncentive.Apprenticeship.LastName &&
                     x.DateOfBirth == storedIncentive.Apprenticeship.DateOfBirth &&
                     x.Uln == storedIncentive.Apprenticeship.UniqueLearnerNumber &&
                     x.EmployerType == storedIncentive.Apprenticeship.EmployerType
                )
                .Should().Be(1);

            var savedPayments = _dbContext.PendingPayments.Where(x => x.ApprenticeshipIncentiveId == storedIncentive.Id);
            savedPayments.Should().BeEquivalentTo(pendingPayments, opt => opt
                .Excluding(x => x.Account)
                .Excluding(x => x.PendingPaymentValidationResultModels));

            var savedValidationResults = _dbContext.PendingPaymentValidationResults.Where(x =>
                x.PendingPaymentId == storedIncentive.PendingPaymentModels.First().Id);
            savedValidationResults.Should().BeEquivalentTo(validationResults, opt => opt
                .Excluding(x => x.DateTime)
                .Excluding(x => x.CollectionPeriod)
            );

            foreach (var result in savedValidationResults)
            {
                result.CollectionPeriodMonth.Should()
                    .Be(validationResults.Single(x => x.Id == result.Id).CollectionPeriod.CalendarMonth);
                result.CollectionPeriodYear.Should()
                    .Be(validationResults.Single(x => x.Id == result.Id).CollectionPeriod.CalendarYear);
            }
        }
    }
}
