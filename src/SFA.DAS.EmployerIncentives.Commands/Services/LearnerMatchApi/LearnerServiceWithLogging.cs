﻿using Microsoft.Extensions.Logging;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives.ValueTypes;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Commands.Services.LearnerMatchApi
{
    public class LearnerServiceWithLogging : ILearnerService
    {
        private readonly ILearnerService _learnerService;
        private readonly ILogger<Learner> _logger;

        public LearnerServiceWithLogging(
            ILearnerService learnerService,
            ILogger<Learner> logger)
        {
            _learnerService = learnerService;
            _logger = logger;
        }

        public async Task Refresh(Learner learner)
        {
            try
            {
                _logger.LogInformation($"Start refresh of learner data from learner match service for ApprenticeshipIncentiveId : {learner.ApprenticeshipIncentiveId},  ApprenticeshipId : {learner.ApprenticeshipId}, Ukprn : {learner.Ukprn}, Url : {learner.UniqueLearnerNumber}");

                await _learnerService.Refresh(learner);

                _logger.LogInformation($"Learner data refresh completed for ApprenticeshipIncentiveId : {learner.ApprenticeshipIncentiveId},  ApprenticeshipId : {learner.ApprenticeshipId}, Ukprn : {learner.Ukprn}, Url : {learner.UniqueLearnerNumber} with result SubmissionFound : {learner.SubmissionFound}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during learner data refresh for ApprenticeshipIncentiveId : {learner.ApprenticeshipIncentiveId},  ApprenticeshipId : {learner.ApprenticeshipId}, Ukprn : {learner.Ukprn}, Url : {learner.UniqueLearnerNumber} with result SubmissionFound : {learner.SubmissionFound}");

                throw;
            }
        }
    }
}
