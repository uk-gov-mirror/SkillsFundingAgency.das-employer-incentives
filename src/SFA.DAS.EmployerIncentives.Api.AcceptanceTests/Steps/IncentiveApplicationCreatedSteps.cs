﻿using System.Data.SqlClient;
using System.Linq;
using FluentAssertions;
using System.Net;
using System.Threading.Tasks;
using AutoFixture;
using Dapper;
using SFA.DAS.EmployerIncentives.Api.Types;
using SFA.DAS.EmployerIncentives.Data.Models;
using TechTalk.SpecFlow;

namespace SFA.DAS.EmployerIncentives.Api.AcceptanceTests.Steps
{
    [Binding]
    [Scope(Feature = "IncentiveApplicationCreated")]
    public class IncentiveApplicationCreatedSteps : StepsBase
    {
        private readonly TestContext _testContext;
        private Fixture _fixture;
        private HttpStatusCode _expectedResult = HttpStatusCode.Created;
        private CreateIncentiveApplicationRequest _request;

        public IncentiveApplicationCreatedSteps(TestContext testContext) : base(testContext)
        {
            _testContext = testContext;
            _fixture = new Fixture();
            _request = _fixture.Create<CreateIncentiveApplicationRequest>();
        }

        [Given(@"I am applying for the New Apprenticeship Incentive")]
        public void GivenIAmApplyingForTheNewApprenticeshipIncentive()
        {
        }

        [When(@"I have selected the apprenticeships for the application")]
        public async Task WhenIHaveSelectedTheApprenticeshipsForTheApplication()
        {
            var url = $"applications";
            await EmployerIncentiveApi.Post(url, _request);
        }

        [Then(@"the application is saved")]
        public async Task ThenTheApplicationIsSaved()
        {
            EmployerIncentiveApi.Response.StatusCode.Should().Be(_expectedResult);

            using (var dbConnection = new SqlConnection(_testContext.SqlDatabase.DatabaseInfo.ConnectionString))
            {
                var application = await dbConnection.QueryAsync<IncentiveApplication>("SELECT * FROM IncentiveApplication WHERE Id = @IncentiveApplicationId",
                    new { _request.IncentiveApplicationId });

                application.Should().HaveCount(1);
                application.Single().Id.Should().Be(_request.IncentiveApplicationId);
            }
        }

    }
}