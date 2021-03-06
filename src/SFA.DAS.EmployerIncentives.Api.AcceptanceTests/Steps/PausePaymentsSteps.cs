using AutoFixture;
using Dapper.Contrib.Extensions;
using FluentAssertions;
using SFA.DAS.EmployerIncentives.Api.Types;
using SFA.DAS.EmployerIncentives.Data.ApprenticeshipIncentives.Models;
using SFA.DAS.EmployerIncentives.Data.Models;
using SFA.DAS.EmployerIncentives.Enums;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TechTalk.SpecFlow;
using System.Net.Http;

namespace SFA.DAS.EmployerIncentives.Api.AcceptanceTests.Steps
{
    [Binding]
    [Scope(Feature = "PausePayments")]
    public class PausePaymentsSteps : StepsBase
    {
        private readonly TestContext _testContext;
        private readonly Fixture _fixture;
        private readonly string _connectionString;
        private readonly IncentiveApplication _application;
        private readonly IncentiveApplicationApprenticeship _apprenticeship;
        private PausePaymentsRequest _pausePaymentsRequest;
        private readonly ApprenticeshipIncentive _apprenticeshipIncentive;
        private HttpResponseMessage _response;

        public PausePaymentsSteps(TestContext testContext) : base(testContext)
        {
            _testContext = testContext;
            _fixture = new Fixture();
            _connectionString = _testContext.SqlDatabase.DatabaseInfo.ConnectionString;

            _application = _fixture.Create<IncentiveApplication>();
            _apprenticeship = _fixture
                .Build<IncentiveApplicationApprenticeship>()
                .With(a => a.IncentiveApplicationId, _application.Id)
                .Create();

            _apprenticeshipIncentive = _fixture
                .Build<ApprenticeshipIncentive>()
                .With(i => i.IncentiveApplicationApprenticeshipId, _apprenticeship.Id)
                .With(a => a.ULN, _apprenticeship.ULN)
                .With(a => a.AccountLegalEntityId, _application.AccountLegalEntityId)
                .With(i => i.PausePayments, false)
                .Create();
        }

        [Given(@"apprenticeship incentive does not exist")]
        public void GivenApprenticeshipIncentiveDoesNotExist()
        {
        }

        [Given(@"an apprenticeship incentive exists")]
        public async Task GivenAnApprenticeshipIncentiveExists()
        {
            using var dbConnection = new SqlConnection(_connectionString);
            await dbConnection.InsertAsync(_application);
            await dbConnection.InsertAsync(_apprenticeship);
            await dbConnection.InsertAsync(_apprenticeshipIncentive);
        }

        [Given(@"a paused apprenticeship incentive exists")]
        public async Task GivenAPausedApprenticeshipIncentiveExists()
        {
            _apprenticeshipIncentive.PausePayments = true;

            using var dbConnection = new SqlConnection(_connectionString);
            await dbConnection.InsertAsync(_application);
            await dbConnection.InsertAsync(_apprenticeship);
            await dbConnection.InsertAsync(_apprenticeshipIncentive);
        }

        [When(@"the pause payments request is sent")]
        public async Task WhenThePausePaymentsRequestIsSent()
        {
            _pausePaymentsRequest = _fixture.Build<PausePaymentsRequest>()
                .With(r => r.Action, PausePaymentsAction.Pause)
                .With(r => r.AccountLegalEntityId, _application.AccountLegalEntityId)
                .With(r => r.ULN, _apprenticeship.ULN)
                .Create();           
            
            var url = "pause-payments";

            _response = await EmployerIncentiveApi.Post(url, _pausePaymentsRequest);
        }

        [Then(@"the requester is informed no apprenticeship incentive is found")]
        public void ThenTheRequesterIsInformedNoApprenticeshipIncentiveIsFound()
        {
            _response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Then(@"the requester is informed the apprenticeship incentive is paused")]
        public async Task ThenTheRequesterIsInformedTheApprenticeshipIncentiveIsPaused()
        {
            _response.StatusCode.Should().Be((int)HttpStatusCode.OK);
            var content = await _response.Content.ReadAsStringAsync();
            JsonConvert.SerializeObject(content).Should().Contain("Payments have been successfully Paused");
        }

        [Then(@"the requester is informed the apprenticeship incentive is already paused")]
        public async Task ThenTheRequesterIsInformedTheApprenticeshipIncentiveIsAlreadyPaused()
        {
            _response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            var content = await _response.Content.ReadAsStringAsync();
            JsonConvert.SerializeObject(content).Should().Contain("Payments already paused");
        }

        [Then(@"the PausePayment status is set to true")]
        public async Task ThenThePausePaymentStatusIsSetToTrue()
        {
            await using var dbConnection = new SqlConnection(_connectionString); 
            var incentives = dbConnection.GetAll<ApprenticeshipIncentive>();

            incentives.Count().Should().Be(1);
            incentives.First().PausePayments.Should().BeTrue();
        }

        [Then(@"an Audit record has been added to record this pause request")]
        public void ThenAnAuditRecordHasBeenAddedToRecordThisPauseRequest()
        {
            using var dbConnection = new SqlConnection(_testContext.SqlDatabase.DatabaseInfo.ConnectionString);
            var statusAudits = dbConnection.GetAll<IncentiveApplicationStatusAudit>();

            statusAudits.Count().Should().Be(1);
            var statusAudit = statusAudits.First();
            statusAudit.Process.Should().Be(IncentiveApplicationStatus.PaymentsPaused);
            statusAudit.IncentiveApplicationApprenticeshipId.Should().Be(_apprenticeshipIncentive.IncentiveApplicationApprenticeshipId);
            statusAudit.ServiceRequestCreatedDate.Should().Be(_pausePaymentsRequest.ServiceRequest.TaskCreatedDate.Value);
            statusAudit.ServiceRequestDecisionReference.Should().Be(_pausePaymentsRequest.ServiceRequest.DecisionReference);
            statusAudit.ServiceRequestTaskId.Should().Be(_pausePaymentsRequest.ServiceRequest.TaskId);
        }
    }
}
