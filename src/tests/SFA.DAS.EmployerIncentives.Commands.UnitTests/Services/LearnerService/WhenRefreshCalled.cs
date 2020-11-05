﻿using AutoFixture;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerIncentives.Commands.Services.LearnerMatchApi;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives.ValueTypes;
using SFA.DAS.EmployerIncentives.UnitTests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Commands.UnitTests.Services.LearnerServiceTests
{
    public class WhenRefreshCalled
    {
        private Commands.Services.LearnerMatchApi.LearnerService _sut;        
        private TestHttpClient _httpClient;
        private Uri _baseAddress;
        private Learner _learner;
        private readonly string _version = "1.0";
        private Fixture _fixture;

        [SetUp]
        public void Arrange()
        {
            _fixture = new Fixture();
            _baseAddress = new Uri(@"http://localhost");
            _httpClient = new TestHttpClient(_baseAddress);

            _learner = new Learner(
                _fixture.Create<Guid>(),
                _fixture.Create<Guid>(),
                _fixture.Create<long>(),
                _fixture.Create<long>(),
                _fixture.Create<long>(),
                _fixture.Create<DateTime>());


            _sut = new Commands.Services.LearnerMatchApi.LearnerService(_httpClient, _version);
        }


        [Test]
        public async Task Then_the_learner_submissionfound_is_false_when_the_learner_data_does_not_exist()
        {
            //Arrange
            _httpClient.SetUpGetAsAsync(System.Net.HttpStatusCode.NotFound);

            //Act
            await _sut.Refresh(_learner);

            //Assert
            _httpClient.VerifyGetAsAsync($"api/v{_version}/{_learner.Ukprn}/{_learner.UniqueLearnerNumber}?", Times.Once());
            _learner.SubmissionFound.Should().BeFalse();
        }

        [Test]
        public async Task Then_the_learner_submissionfound_is_true_when_the_learner_data_does_not_exist()
        {
            //Arrange
            var learnerSubmissionDto = _fixture.Create<LearnerSubmissionDto>();

            _httpClient.SetUpGetAsAsync(learnerSubmissionDto, System.Net.HttpStatusCode.OK);

            //Act
            await _sut.Refresh(_learner);

            //Assert
            _httpClient.VerifyGetAsAsync($"api/v{_version}/{_learner.Ukprn}/{_learner.UniqueLearnerNumber}?", Times.Once());
            _learner.SubmissionFound.Should().BeTrue();
        }

        [Test]
        public async Task Then_the_learning_found_is_false_when_there_are_no_matching_training_entries_returned_from_the_matching_service()
        {
            //Arrange
            var learnerSubmissionDto = _fixture.Create<LearnerSubmissionDto>();

            _httpClient.SetUpGetAsAsync(learnerSubmissionDto, System.Net.HttpStatusCode.OK);

            //Act
            await _sut.Refresh(_learner);

            //Assert
            _learner.SubmissionData.LearningFound.Should().BeFalse();
        }

        [Test]
        public async Task Then_the_learning_found_is_true_when_there_are_matching_training_entries_returned_from_the_matching_service()
        {
            //Arrange
            var learnerSubmissionDto = _fixture
                .Build<LearnerSubmissionDto>()
                .With(l => l.Training, new List<TrainingDto> {
                    _fixture.Create<TrainingDto>(),
                    _fixture
                        .Build<TrainingDto>()
                        .With(p => p.Reference, "ZPROG001")
                        .Create(),
                    _fixture.Create<TrainingDto>()
                    })
                .Create();

            _httpClient.SetUpGetAsAsync(learnerSubmissionDto, System.Net.HttpStatusCode.OK);

            //Act
            await _sut.Refresh(_learner);

            //Assert
            _learner.SubmissionData.LearningFound.Should().BeTrue();
        }

        [Test]
        public async Task Then_the_submission_date_is_true_when_the_learner_data_exists()
        {
            //Arrange
            var testDate = DateTime.Now;

            var learnerSubmissionDto = _fixture
                .Build<LearnerSubmissionDto>()
                .With(p => p.IlrSubmissionDate, testDate)                
                .Create();

            _httpClient.SetUpGetAsAsync(learnerSubmissionDto, System.Net.HttpStatusCode.OK);

            //Act
            await _sut.Refresh(_learner);

            //Assert
            _learner.SubmissionData.SubmissionDate.Should().Be(testDate);
        }
    }
}