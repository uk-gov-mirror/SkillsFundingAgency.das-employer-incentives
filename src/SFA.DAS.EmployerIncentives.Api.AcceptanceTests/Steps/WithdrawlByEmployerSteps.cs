﻿using AutoFixture;
using Dapper.Contrib.Extensions;
using FluentAssertions;
using NServiceBus.Transport;
using SFA.DAS.EmployerIncentives.Api.Types;
using SFA.DAS.EmployerIncentives.Commands.Types.ApprenticeshipIncentive;
using SFA.DAS.EmployerIncentives.Data.ApprenticeshipIncentives.Models;
using SFA.DAS.EmployerIncentives.Data.Models;
using SFA.DAS.EmployerIncentives.Enums;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace SFA.DAS.EmployerIncentives.Api.AcceptanceTests.Steps
{
    [Binding]
    [Scope(Feature = "WithdrawlByEmployer")]
    public class WithdrawlByEmployerSteps : StepsBase
    {
        private readonly TestContext _testContext;
        private readonly Fixture _fixture;
        private readonly string _connectionString;
        private readonly IncentiveApplication _application;
        private readonly IncentiveApplicationApprenticeship _apprenticeship;
        private WithdrawApplicationRequest _withdrawApplicationRequest;
        private readonly IncentiveApplicationApprenticeship _apprenticeship2;

        private readonly ApprenticeshipIncentive _apprenticeshipIncentive;
        private readonly PendingPayment _pendingPayment;

        public WithdrawlByEmployerSteps(TestContext testContext) : base(testContext)
        {
            _testContext = testContext;
            _fixture = new Fixture();
            _connectionString = _testContext.SqlDatabase.DatabaseInfo.ConnectionString;

            _application = _fixture.Create<IncentiveApplication>();
            _apprenticeship = _fixture
                .Build<IncentiveApplicationApprenticeship>()
                .With(a => a.IncentiveApplicationId, _application.Id)
                .With(a => a.WithdrawnByEmployer, false)
                .Create();

            _apprenticeship2 = _fixture
                .Build<IncentiveApplicationApprenticeship>()
                .With(a => a.IncentiveApplicationId, _application.Id)
                .With(a => a.ULN, _apprenticeship.ULN)
                .With(a => a.WithdrawnByEmployer, false)
                .Create();

            _apprenticeshipIncentive = _fixture
                .Build<ApprenticeshipIncentive>()
                .With(i => i.IncentiveApplicationApprenticeshipId, _apprenticeship.Id)
                .Create();

            _pendingPayment = _fixture
                .Build<PendingPayment>()
                .With(p => p.ApprenticeshipIncentiveId, _apprenticeshipIncentive.Id)
                .Create();
        }

        [Given(@"an incentive application has been made without being submitted")]
        public async Task GivenAnIncentiveApplicationHasBeenMadeWithoutBeingSubmitted()
        {
            using var dbConnection = new SqlConnection(_connectionString);
            await dbConnection.InsertAsync(_application);
            await dbConnection.InsertAsync(_apprenticeship);
        }

        [Given(@"multiple incentive applications have been made for the same ULN without being submitted")]
        public async Task GivenMultiplwIncentiveApplicationsHaveBeenMadeWithoutBeingSubmitted()
        {            
            using var dbConnection = new SqlConnection(_connectionString);
            await dbConnection.InsertAsync(_application);
            await dbConnection.InsertAsync(_apprenticeship);
            await dbConnection.InsertAsync(_apprenticeship2);
        }

        [Given(@"an apprenticeship incentive with pending payments exists as a result of an incentive application")]
        public async Task GivenAnApprenticeshipIncentiveWithPendingPaymentsExistsForAnApplication()
        {
            using var dbConnection = new SqlConnection(_connectionString);
            await dbConnection.InsertAsync(_application);
            await dbConnection.InsertAsync(_apprenticeship);
            await dbConnection.InsertAsync(_apprenticeshipIncentive);
            await dbConnection.InsertAsync(_pendingPayment);
        }       


        [When(@"the apprenticeship application is withdrawn from the scheme")]
        public async Task WhenTheApprenticeshipApplicationIsWithdrawnFromTheScheme()
        {
            _withdrawApplicationRequest = _fixture
                .Build<WithdrawApplicationRequest>()
                .With(r => r.WithdrawlType, WithdrawlType.Employer)
                .With(r => r.AccountLegalEntityId, _application.AccountLegalEntityId)
                .With(r => r.ULN, _apprenticeship.ULN)
                .Create();           
            
            var url = $"withdrawls";

            await _testContext.WaitFor<MessageContext>(async () =>
                     await EmployerIncentiveApi.Post(url, _withdrawApplicationRequest));
        }             

        [Then(@"the incentive application status is updated to indicate the employer withdrawl")]
        public async Task ThenTheIncentiveApplicationStatusIsUpdatedToIndicateTheEmployerWithdrawl()
        {
            EmployerIncentiveApi.Response.StatusCode.Should().Be(HttpStatusCode.Accepted);

            await using var dbConnection = new SqlConnection(_connectionString);
            var apprenticeships = await dbConnection.GetAllAsync<IncentiveApplicationApprenticeship>();
            apprenticeships.Should().HaveCount(1);
            apprenticeships.Single(a => a.Id == _apprenticeship.Id).WithdrawnByEmployer.Should().BeTrue();
            
            var incentiveApplicationAudits = await dbConnection.GetAllAsync<IncentiveApplicationStatusAudit>();
            incentiveApplicationAudits.Should().HaveCount(1);
            var auditRecord = incentiveApplicationAudits.Single(a => a.IncentiveApplicationApprenticeshipId == _apprenticeship.Id);
            auditRecord.Process.Should().Be(IncentiveApplicationStatus.EmployerWithdrawn);
            auditRecord.ServiceRequestTaskId.Should().Be(_withdrawApplicationRequest.ServiceRequestTaskId);
            auditRecord.ServiceRequestDecisionReference.Should().Be(_withdrawApplicationRequest.ServiceRequestDecisionNumber);
            auditRecord.ServiceRequestCreatedDate.Should().Be(_withdrawApplicationRequest.ServiceRequestCreatedDate);

            var publishedCommand = _testContext
                .CommandsPublished
                .Single(c => c.IsPublished &&
                c.Command is WithdrawCommand).Command as WithdrawCommand;
                       
            publishedCommand.AccountId.Should().Be(_application.AccountId);
            publishedCommand.IncentiveApplicationApprenticeshipId.Should().Be(_apprenticeship.Id);
        }

        [Then(@"each incentive application status is updated to indicate the employer withdrawl")]
        public async Task ThenEachIncentiveApplicationStatusIsUpdatedToIndicateTheEmployerWithdrawl()
        {
            EmployerIncentiveApi.Response.StatusCode.Should().Be(HttpStatusCode.Accepted);

            await using var dbConnection = new SqlConnection(_connectionString);
            var apprenticeships = await dbConnection.GetAllAsync<IncentiveApplicationApprenticeship>();
            apprenticeships.Should().HaveCount(2);
            apprenticeships.Single(a => a.Id == _apprenticeship.Id).WithdrawnByEmployer.Should().BeTrue();
            apprenticeships.Single(a => a.Id == _apprenticeship2.Id).WithdrawnByEmployer.Should().BeTrue();

            var incentiveApplicationAudits = await dbConnection.GetAllAsync<IncentiveApplicationStatusAudit>();
            incentiveApplicationAudits.Should().HaveCount(2);

            incentiveApplicationAudits.Single(a => a.IncentiveApplicationApprenticeshipId == _apprenticeship.Id).Process.Should().Be(IncentiveApplicationStatus.EmployerWithdrawn);
            incentiveApplicationAudits.Single(a => a.IncentiveApplicationApprenticeshipId == _apprenticeship2.Id).Process.Should().Be(IncentiveApplicationStatus.EmployerWithdrawn);

            _testContext
                .CommandsPublished
                .Count(c => c.IsPublished &&
                c.Command is WithdrawCommand)
                .Should().Be(2);
        }
        [Then(@"the apprenticeship incentive and it's pending payments are removed from the system")]
        public async Task ThenTheIncentiveAndPendingPaymentsAreremovedFromTheSystem()
        {
            await ThenTheIncentiveApplicationStatusIsUpdatedToIndicateTheEmployerWithdrawl();

            await using var dbConnection = new SqlConnection(_connectionString);
            var incentives = await dbConnection.GetAllAsync<ApprenticeshipIncentive>();
            var pendingPayments = await dbConnection.GetAllAsync<PendingPayment>();

            incentives.Should().HaveCount(0);
            pendingPayments.Should().HaveCount(0);
        }        
    }
}