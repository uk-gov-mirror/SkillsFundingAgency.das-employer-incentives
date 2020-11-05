﻿using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SFA.DAS.EmployerIncentives.Data.Models;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives.ValueTypes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Data.UnitTests.ApprenticeshipIncentive
{
    public class WhenSaveCalled
    {
        private ApprenticeshipIncentives.LearnerDataRepository _sut;
        private Fixture _fixture;
        private EmployerIncentivesDbContext _dbContext;

        [SetUp]
        public void Arrange()
        {
            _fixture = new Fixture();

            var options = new DbContextOptionsBuilder<EmployerIncentivesDbContext>()
                .UseInMemoryDatabase("EmployerIncentivesDbContext" + Guid.NewGuid()).Options;
            _dbContext = new EmployerIncentivesDbContext(options);

            _sut = new ApprenticeshipIncentives.LearnerDataRepository(_dbContext);
        }

        [TearDown]
        public void CleanUp()
        {            
            _dbContext.Dispose();
         }

        [Test]
        public async Task Then_the_learner_is_added_to_the_data_store()
        {
            // Arrange
            var testLearner = _fixture.Create<Learner>();
            var submissionData = _fixture.Create<SubmissionData>();
            submissionData.SetRawJson(_fixture.Create<string>());
            testLearner.SetSubmissionData(submissionData);

            // Act
            await _sut.Save(testLearner);
            await _dbContext.SaveChangesAsync();

            // Assert
            _dbContext.Learners.Count().Should().Be(1);

            var storedLearner = _dbContext.Learners.Single();
            storedLearner.Id.Should().Be(testLearner.Id);
            storedLearner.ApprenticeshipIncentiveId.Should().Be(testLearner.ApprenticeshipIncentiveId);
            storedLearner.ApprenticeshipId.Should().Be(testLearner.ApprenticeshipId);
            storedLearner.Ukprn.Should().Be(testLearner.Ukprn);
            storedLearner.ULN.Should().Be(testLearner.UniqueLearnerNumber);
            storedLearner.SubmissionFound.Should().Be(testLearner.SubmissionFound);
            storedLearner.SubmissionDate.Should().Be(testLearner.SubmissionData.SubmissionDate);
            storedLearner.LearningFound.Should().Be(testLearner.SubmissionData.LearningFound);
            storedLearner.HasDataLock.Should().BeNull();
            storedLearner.StartDate.Should().BeNull();
            storedLearner.DaysInLearning.Should().BeNull();
            storedLearner.InLearning.Should().BeNull();
            storedLearner.RawJSON.Should().Be(testLearner.SubmissionData.RawJson);
            storedLearner.CreatedDate.Should().Be(testLearner.CreatedDate);
        }

        [Test]
        public async Task Then_an_existing_learner_is_updated_in_the_data_store()
        {
            // Arrange
            var existingLearner = _fixture.Create<ApprenticeshipIncentives.Models.Learner>();
            _dbContext.Learners.Add(existingLearner);
            await _dbContext.SaveChangesAsync();

            var testLearner = new Learner(existingLearner.Id, _fixture.Create<Guid>(), _fixture.Create<long>(), _fixture.Create<long>(), _fixture.Create<long>(), _fixture.Create<DateTime>());
            var submissionData = _fixture.Create<SubmissionData>();
            submissionData.SetRawJson(_fixture.Create<string>());
            testLearner.SetSubmissionData(submissionData);

            // Act
            await _sut.Save(testLearner);
            await _dbContext.SaveChangesAsync();

            // Assert
            _dbContext.Learners.Count().Should().Be(1);

            var storedLearner = _dbContext.Learners.Single();
            storedLearner.Id.Should().Be(testLearner.Id);
            storedLearner.ApprenticeshipIncentiveId.Should().Be(testLearner.ApprenticeshipIncentiveId);
            storedLearner.ApprenticeshipId.Should().Be(testLearner.ApprenticeshipId);
            storedLearner.Ukprn.Should().Be(testLearner.Ukprn);
            storedLearner.ULN.Should().Be(testLearner.UniqueLearnerNumber);
            storedLearner.SubmissionFound.Should().Be(testLearner.SubmissionFound);
            storedLearner.SubmissionDate.Should().Be(testLearner.SubmissionData.SubmissionDate);
            storedLearner.LearningFound.Should().Be(testLearner.SubmissionData.LearningFound);
            storedLearner.HasDataLock.Should().BeNull();
            storedLearner.StartDate.Should().BeNull();
            storedLearner.DaysInLearning.Should().BeNull();
            storedLearner.InLearning.Should().BeNull();
            storedLearner.RawJSON.Should().Be(testLearner.SubmissionData.RawJson);
            storedLearner.CreatedDate.Should().Be(testLearner.CreatedDate);
        }
    }
}