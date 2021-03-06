using AutoFixture;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerIncentives.Application.UnitTests;
using SFA.DAS.EmployerIncentives.Commands.ApprenticeshipIncentive.RefreshLearner;
using SFA.DAS.EmployerIncentives.Commands.Persistence;
using SFA.DAS.EmployerIncentives.Commands.Services.LearnerMatchApi;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives.Models;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives.ValueTypes;
using SFA.DAS.EmployerIncentives.Domain.Factories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Commands.UnitTests.RefreshLearner.Handlers
{
    public class WhenHandlingRefreshLearnerCommand
    {
        private RefreshLearnerCommandHandler _sut;
        private Mock<IApprenticeshipIncentiveDomainRepository> _mockApprenticeshipIncentiveDomainRepository;
        private Mock<ILearnerDomainRepository> _mockLearnerDomainRepository;
        private Mock<ILearnerService> _mockLearnerService;
        private Mock<ILogger<RefreshLearnerCommandHandler>> _mockLogger;
        private Fixture _fixture;
        private Guid _apprenticeshipIncentiveId;
        private LearnerSubmissionDto _learnerSubmissionDto;
        private Domain.ApprenticeshipIncentives.ApprenticeshipIncentive _apprenticeshipIncentive;
        private ApprenticeshipIncentiveModel _incentiveModel;
        private Learner _learner;
        private DateTime _testStartDate;
        private DateTime _censusDate;

        [SetUp]
        public void Arrange()
        {
            _fixture = new Fixture();
            _testStartDate = _fixture.Create<DateTime>();
            _censusDate = _testStartDate.AddDays(5);

            _mockApprenticeshipIncentiveDomainRepository = new Mock<IApprenticeshipIncentiveDomainRepository>();
            _mockLearnerDomainRepository = new Mock<ILearnerDomainRepository>();
            _mockLearnerService = new Mock<ILearnerService>();
            _mockLogger = new Mock<ILogger<RefreshLearnerCommandHandler>>();

            var apprenticeship = _fixture.Create<Apprenticeship>();
            apprenticeship.SetProvider(_fixture.Create<Provider>());

            _incentiveModel = _fixture.Build<ApprenticeshipIncentiveModel>()
                .With(p => p.Apprenticeship, apprenticeship)
                .Create();

            _apprenticeshipIncentiveId = _incentiveModel.Id;
            _apprenticeshipIncentive = new ApprenticeshipIncentiveFactory().GetExisting(_incentiveModel.Id, _incentiveModel);

            _mockApprenticeshipIncentiveDomainRepository
                .Setup(m => m.Find(_apprenticeshipIncentiveId))
                .ReturnsAsync(_apprenticeshipIncentive);

            _learnerSubmissionDto = _fixture.Create<LearnerSubmissionDto>();

            _mockLearnerService
                .Setup(m => m.Get(It.IsAny<Learner>()))
                .ReturnsAsync(_learnerSubmissionDto);

            _mockApprenticeshipIncentiveDomainRepository
                .Setup(m => m.Find(_apprenticeshipIncentiveId))
                .ReturnsAsync(_apprenticeshipIncentive);

            _learner = new LearnerFactory().GetExisting(_fixture.Create<LearnerModel>());

            _mockLearnerDomainRepository
                .Setup(m => m.GetOrCreate(_apprenticeshipIncentive))
                .ReturnsAsync(_learner);

            _sut = new RefreshLearnerCommandHandler(
                _mockLogger.Object,
                _mockApprenticeshipIncentiveDomainRepository.Object,
                _mockLearnerService.Object,
                _mockLearnerDomainRepository.Object);
        }

        [Test]
        public async Task Then_a_Learner_record_is_created_when_it_doesnt_already_exist()
        {
            // Arrange
            var command = new RefreshLearnerCommand(_apprenticeshipIncentiveId);

            // Act
            await _sut.Handle(command);

            // Assert
            _mockLearnerDomainRepository.Verify(m => m.Save(It.IsAny<Learner>()), Times.Once);
        }

        [Test]
        public async Task Then_a_Learner_record_is_updated_when_it_already_exist()
        {
            // Arrange
            var command = new RefreshLearnerCommand(_apprenticeshipIncentiveId);

            // Act
            await _sut.Handle(command);

            // Assert
            _mockLearnerDomainRepository.Verify(m => m.Save(_learner), Times.Once);
        }

        [Test]
        public async Task Then_the_learner_submissionfound_is_false_when_the_learner_data_does_not_exist()
        {
            //Arrange
            var command = new RefreshLearnerCommand(_apprenticeshipIncentiveId);

            _mockLearnerService
                .Setup(m => m.Get(It.IsAny<Learner>()))
                .ReturnsAsync(null as LearnerSubmissionDto);

            //Act
            await _sut.Handle(command);

            //Assert
            _mockLearnerDomainRepository.Verify(m => m.Save(
                It.Is<Learner>(l => !l.SubmissionData.SubmissionFound)
                ), Times.Once);
        }

        [Test]
        public async Task Then_the_learner_submissionfound_is_true_when_the_learner_data_does_exist()
        {
            //Arrange
            var command = new RefreshLearnerCommand(_apprenticeshipIncentiveId);
            var learnerSubmissionDto = _fixture.Create<LearnerSubmissionDto>();

            _mockLearnerService
                .Setup(m => m.Get(It.IsAny<Learner>()))
                .ReturnsAsync(learnerSubmissionDto);

            //Act
            await _sut.Handle(command);

            //Assert
            _mockLearnerDomainRepository.Verify(m => m.Save(
                It.Is<Learner>(l => l.SubmissionData.SubmissionFound)
                ), Times.Once);
        }

        [Test]
        public async Task Then_the_learning_found_is_false_when_there_are_no_matching_training_entries_returned_from_the_learner_service()
        {
            //Arrange
            var command = new RefreshLearnerCommand(_apprenticeshipIncentiveId);
            var learnerSubmissionDto = _fixture.Create<LearnerSubmissionDto>();

            _mockLearnerService
                .Setup(m => m.Get(It.IsAny<Learner>()))
                .ReturnsAsync(learnerSubmissionDto);

            //Act
            await _sut.Handle(command);

            //Assert
            _mockLearnerDomainRepository.Verify(m => m.Save(
                It.Is<Learner>(l => !l.SubmissionData.LearningData.LearningFound)
                ), Times.Once);
        }

        [Test]
        public async Task Then_the_learning_found_is_true_when_there_are_matching_training_entries_returned_from_the_learner_service()
        {
            //Arrange
            var command = new RefreshLearnerCommand(_apprenticeshipIncentiveId);
            var learnerSubmissionDto = _fixture
                        .Build<LearnerSubmissionDto>()
                        .With(l => l.Training, new List<TrainingDto> {
                            _fixture.Create<TrainingDto>(),
                            _fixture
                                .Build<TrainingDto>()
                                .With(p => p.Reference, "ZPROG001")
                                .With(p => p.PriceEpisodes, new List<PriceEpisodeDto>(){
                                    _fixture.Build<PriceEpisodeDto>()
                                    .With(pe => pe.Periods, new List<PeriodDto>(){
                                        _fixture.Build<PeriodDto>()
                                        .With(p => p.ApprenticeshipId, _apprenticeshipIncentive.Apprenticeship.Id)
                                        .Create()
                                        })
                                    .With(pe => pe.StartDate, _testStartDate)
                                    .With(pe => pe.EndDate, _testStartDate.AddDays(2))
                                    .Create()
                                })
                                .Create(),
                            _fixture.Create<TrainingDto>()
                            })
                        .Create();

            _mockLearnerService
                .Setup(m => m.Get(It.IsAny<Learner>()))
                .ReturnsAsync(learnerSubmissionDto);

            _censusDate = _testStartDate.AddDays(5);

            //Act
            await _sut.Handle(command);

            //Assert
            _mockLearnerDomainRepository.Verify(m => m.Save(
                It.Is<Learner>(l => l.SubmissionData.LearningData.LearningFound)
                ), Times.Once);
        }

        [Test]
        public async Task Then_the_submission_date_is_true_the_learner_data_exists()
        {
            //Arrange
            var command = new RefreshLearnerCommand(_apprenticeshipIncentiveId);
            var testDate = DateTime.Now.AddDays(-1);

            var learnerSubmissionDto = _fixture
                        .Build<LearnerSubmissionDto>()
                        .With(l => l.IlrSubmissionDate, testDate)
                        .Create();

            _mockLearnerService
                .Setup(m => m.Get(It.IsAny<Learner>()))
                .ReturnsAsync(learnerSubmissionDto);

            //Act
            await _sut.Handle(command);

            //Assert
            _mockLearnerDomainRepository.Verify(m => m.Save(
                It.Is<Learner>(l => l.SubmissionData.SubmissionDate == testDate)
                ), Times.Once);
        }

        [Test]
        public async Task Then_the_startdate_is_set_when_one_exists_for_the_learner_data()
        {
            //Arrange            

            var command = new RefreshLearnerCommand(_apprenticeshipIncentiveId);

            var learnerSubmissionDto = _fixture
                .Build<LearnerSubmissionDto>()
                .With(l => l.Training, new List<TrainingDto> {
                    _fixture.Create<TrainingDto>(),
                    _fixture
                        .Build<TrainingDto>()
                        .With(p => p.Reference, "ZPROG001")
                        .With(p => p.PriceEpisodes, new List<PriceEpisodeDto>(){_fixture.Build<PriceEpisodeDto>()
                                                    .With(pe => pe.Periods, new List<PeriodDto>(){
                                                        _fixture.Build<PeriodDto>()
                                                       .With(period => period.ApprenticeshipId, _incentiveModel.Apprenticeship.Id)
                                                       .With(period => period.IsPayable, true)
                                                       .Create()
                                                    })
                                                    .With(pe => pe.StartDate, _testStartDate)
                                                    .With(pe => pe.EndDate, _testStartDate.AddDays(2))
                                                    .Create() }
                        )
                        .Create(),
                    _fixture.Create<TrainingDto>() }
                    )
                .Create();

            _mockLearnerService
                .Setup(m => m.Get(It.IsAny<Learner>()))
                .ReturnsAsync(learnerSubmissionDto);

            _censusDate = _testStartDate.AddDays(5);

            //Act
            await _sut.Handle(command);

            //Assert
            _mockLearnerDomainRepository.Verify(m => m.Save(
                It.Is<Learner>(l => l.SubmissionData.LearningData.StartDate == _testStartDate)
                ), Times.Once);
        }

        [Test]
        public async Task Then_the_inlearning_flag_is_null_when_there_are_no_pending_payments()
        {
            //Arrange
            var command = new RefreshLearnerCommand(_apprenticeshipIncentiveId);

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

            _mockLearnerService
                .Setup(m => m.Get(It.IsAny<Learner>()))
                .ReturnsAsync(learnerSubmissionDto);

            //Act
            await _sut.Handle(command);

            //Assert
            _mockLearnerDomainRepository.Verify(m => m.Save(
                It.Is<Learner>(l => l.SubmissionData.LearningData.IsInlearning == null)
                ), Times.Once);
        }

        [Test]
        public async Task Then_the_inlearning_flag_is_true_when_there_is_a_pending_payment_with_a_matching_inlearning_period()
        {
            //Arrange
            var command = new RefreshLearnerCommand(_apprenticeshipIncentiveId);

            var apprenticeship = _fixture.Create<Apprenticeship>();
            apprenticeship.SetProvider(_fixture.Create<Provider>());

            var pendingPaymentModel = _fixture.Create<PendingPaymentModel>();
            pendingPaymentModel.PaymentMadeDate = null;

            var apprenticeshipIncentiveModel = _fixture.Build<ApprenticeshipIncentiveModel>()
               .With(p => p.Apprenticeship, apprenticeship)
               .With(p => p.PendingPaymentModels, new List<PendingPaymentModel> { pendingPaymentModel })
               .Create();

            _mockApprenticeshipIncentiveDomainRepository
                .Setup(m => m.Find(_apprenticeshipIncentiveId))
                .ReturnsAsync(new ApprenticeshipIncentiveFactory().GetExisting(apprenticeshipIncentiveModel.ApplicationApprenticeshipId, apprenticeshipIncentiveModel));

            var apprenticeshipIncentive = new ApprenticeshipIncentiveFactory().GetExisting(apprenticeshipIncentiveModel.Id, apprenticeshipIncentiveModel);

            var learner = new LearnerFactory().GetExisting(_fixture.Create<LearnerModel>());

            _mockLearnerDomainRepository
                .Setup(m => m.GetOrCreate(apprenticeshipIncentive))
                .ReturnsAsync(learner);

            var learnerSubmissionDto = _fixture
               .Build<LearnerSubmissionDto>()
               .With(l => l.Training, new List<TrainingDto> {
                    _fixture.Create<TrainingDto>(),
                    _fixture
                        .Build<TrainingDto>()
                        .With(p => p.Reference, "ZPROG001")
                        .With(t => t.PriceEpisodes,
                        new List<PriceEpisodeDto>{
                            _fixture
                            .Build<PriceEpisodeDto>()
                            .With(pe => pe.StartDate, pendingPaymentModel.DueDate.AddDays(-1))
                            .With(pe => pe.EndDate, pendingPaymentModel.DueDate.AddDays(1))
                            .With(pe => pe.Periods,
                                    new List<PeriodDto>{
                                        _fixture
                                        .Build<PeriodDto>()
                                        .With(p => p.Period, pendingPaymentModel.PeriodNumber)
                                        .With(p => p.ApprenticeshipId, apprenticeshipIncentive.Apprenticeship.Id)
                                        .Create()
                                    })
                            .Create()
                            }).Create(),
                        _fixture.Create<TrainingDto>()
                   })
               .Create();

            _mockLearnerService
                .Setup(m => m.Get(It.IsAny<Learner>()))
                .ReturnsAsync(learnerSubmissionDto);

            //Act
            await _sut.Handle(command);

            //Assert
            _mockLearnerDomainRepository.Verify(m => m.Save(
                It.Is<Learner>(l => l.SubmissionData.LearningData.IsInlearning == true)
                ), Times.Once);
        }

        [Test]
        public async Task Then_the_LearningPeriods_are_set_when_there_are_price_episodes_with_periods_for_the_apprenticeship()
        {
            //Arrange
            var command = new RefreshLearnerCommand(_apprenticeshipIncentiveId);

            var apprenticeship = _fixture.Create<Apprenticeship>();
            apprenticeship.SetProvider(_fixture.Create<Provider>());

            var apprenticeshipIncentiveModel = _fixture.Build<ApprenticeshipIncentiveModel>()
               .With(p => p.Apprenticeship, apprenticeship)
               .Create();

            _mockApprenticeshipIncentiveDomainRepository
                .Setup(m => m.Find(_apprenticeshipIncentiveId))
                .ReturnsAsync(new ApprenticeshipIncentiveFactory().GetExisting(apprenticeshipIncentiveModel.ApplicationApprenticeshipId, apprenticeshipIncentiveModel));

            var apprenticeshipIncentive = new ApprenticeshipIncentiveFactory().GetExisting(apprenticeshipIncentiveModel.Id, apprenticeshipIncentiveModel);

            var learner = new LearnerFactory().GetExisting(_fixture.Create<LearnerModel>());

            _mockLearnerDomainRepository
                .Setup(m => m.GetOrCreate(apprenticeshipIncentive))
                .ReturnsAsync(learner);

            var learnerSubmissionDto = _fixture
               .Build<LearnerSubmissionDto>()
               .With(l => l.Training, new List<TrainingDto> {
                    _fixture.Create<TrainingDto>(),
                    _fixture
                        .Build<TrainingDto>()
                        .With(p => p.Reference, "ZPROG001")
                        .With(t => t.PriceEpisodes,
                        new List<PriceEpisodeDto>{
                            _fixture
                            .Build<PriceEpisodeDto>()
                            .With(pe => pe.Periods,
                                    new List<PeriodDto>{
                                        _fixture
                                        .Build<PeriodDto>()
                                        .With(p => p.ApprenticeshipId, apprenticeshipIncentive.Apprenticeship.Id)
                                        .Create()
                                    })
                            .Create()
                            }).Create(),
                        _fixture.Create<TrainingDto>()
                   })
               .Create();

            _mockLearnerService
                .Setup(m => m.Get(It.IsAny<Learner>()))
                .ReturnsAsync(learnerSubmissionDto);

            //Act
            await _sut.Handle(command);

            //Assert
            _mockLearnerDomainRepository.Verify(m => m.Save(
                It.Is<Learner>(l => l.GetModel().LearningPeriods.Count == 1)
                ), Times.Once);
        }

        [Test]
        public async Task Then_the_learning_not_found_reason_is_logged() // EI-490
        {
            //Arrange
            var command = new RefreshLearnerCommand(_apprenticeshipIncentiveId);
            var learnerSubmissionDto = _fixture.Create<LearnerSubmissionDto>();

            _mockLearnerService
                .Setup(m => m.Get(It.IsAny<Learner>()))
                .ReturnsAsync(learnerSubmissionDto);

            //Act
            await _sut.Handle(command);

            //Assert
            _mockLogger.VerifyLogContains(LogLevel.Information, Times.Once(), $"Start Learner data refresh from Learner match service for ApprenticeshipIncentiveId: {_learner.ApprenticeshipIncentiveId}, ApprenticeshipId: {_learner.ApprenticeshipId}, UKPRN: {_learner.Ukprn}, ULN: {_learner.UniqueLearnerNumber}");
            _mockLogger.VerifyLogContains(LogLevel.Information, Times.Once(), $"End Learner data refresh from Learner match service for ApprenticeshipIncentiveId: {_learner.ApprenticeshipIncentiveId}, ApprenticeshipId: {_learner.ApprenticeshipId}, UKPRN: {_learner.Ukprn}, ULN: {_learner.UniqueLearnerNumber}");
            _mockLogger.VerifyLogContains(LogLevel.Information, Times.Once(), $"Matching ILR record not found for ApprenticeshipIncentiveId: {_learner.ApprenticeshipIncentiveId}, ApprenticeshipId: {_learner.ApprenticeshipId}, UKPRN: {_learner.Ukprn}, ULN: {_learner.UniqueLearnerNumber} with reason: {_learner.SubmissionData.LearningData.NotFoundReason}");

        }
    }
}
