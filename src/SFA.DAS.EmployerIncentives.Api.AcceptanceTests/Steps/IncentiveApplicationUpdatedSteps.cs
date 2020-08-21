﻿using AutoFixture;
using CSScriptLib;
using Dapper;
using FluentAssertions;
using SFA.DAS.EmployerIncentives.Abstractions.DTOs.Commands;
using SFA.DAS.EmployerIncentives.Api.Types;
using SFA.DAS.EmployerIncentives.Data.Models;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace SFA.DAS.EmployerIncentives.Api.AcceptanceTests.Steps
{
    [Binding]
    [Scope(Feature = "IncentiveApplicationUpdated")]
    public class IncentiveApplicationUpdatedSteps : StepsBase
    {
        private readonly TestContext _testContext;
        private UpdateIncentiveApplicationRequest _updateApplicationRequest;
        private CreateIncentiveApplicationRequest _createApplicationRequest;

        public IncentiveApplicationUpdatedSteps(TestContext testContext) : base(testContext)
        {
            _testContext = testContext;
        }

        [Given(@"An employer is applying for the New Apprenticeship Incentive")]
        public async Task GivenAnEmployerIsApplyingForTheNewApprenticeshipIncentive()
        {
            _createApplicationRequest = Fixture.Create<CreateIncentiveApplicationRequest>();
            const string url = "applications";
            await EmployerIncentiveApi.Post(url, _createApplicationRequest);


        }

        [When(@"They have changed selected apprenticeships for the application")]
        public async Task WhenTheyHaveChangedSelectedApprenticeshipsForTheApplication()
        {
            _updateApplicationRequest = new UpdateIncentiveApplicationRequest()
            {
                IncentiveApplicationId = _createApplicationRequest.IncentiveApplicationId,
                Apprenticeships = Fixture.CreateMany<IncentiveApplicationApprenticeshipDto>(4),
                AccountId = _createApplicationRequest.AccountId,
            };
            _updateApplicationRequest.Apprenticeships.AddItem(_createApplicationRequest.Apprenticeships.First());

            var url = $"applications/{_updateApplicationRequest.IncentiveApplicationId}";
            await EmployerIncentiveApi.Put(url, _updateApplicationRequest);
        }

        [Then(@"the application is updated with new selection of apprenticeships")]
        public void ThenTheApplicationIsUpdatedWithNewSelectionOfApprenticeships()
        {
            EmployerIncentiveApi.Response.StatusCode.Should().Be(HttpStatusCode.OK);

            using var dbConnection = new SqlConnection(_testContext.SqlDatabase.DatabaseInfo.ConnectionString);
            var query = $"SELECT * FROM IncentiveApplicationApprenticeship WHERE IncentiveApplicationId = '{ _updateApplicationRequest.IncentiveApplicationId}'";
            var apprenticeships = dbConnection.Query<IncentiveApplicationApprenticeship>(query).ToList();

            apprenticeships.Should().BeEquivalentTo(_updateApplicationRequest.Apprenticeships);
        }

    }
}