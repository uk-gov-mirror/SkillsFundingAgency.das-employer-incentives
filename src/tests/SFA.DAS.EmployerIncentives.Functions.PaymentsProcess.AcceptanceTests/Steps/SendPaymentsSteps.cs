using Dapper.Contrib.Extensions;
using FluentAssertions;
using SFA.DAS.EmployerIncentives.Data.ApprenticeshipIncentives.Models;
using SFA.DAS.EmployerIncentives.Functions.PaymentsProcess.Orchestrators;
using SFA.DAS.EmployerIncentives.Functions.TestHelpers;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace SFA.DAS.EmployerIncentives.Functions.PaymentsProcess.AcceptanceTests.Steps
{
    [Binding]
    [Scope(Feature = "SendPayments")]
    public partial class SendPaymentsSteps
    {
        private readonly TestContext _testContext;
        private ValidatePaymentsSteps.ValidatePaymentData _validatePaymentData;
        private string _orchestratorInstanceId;

        private const short CollectionPeriodYear = 2021;
        private const byte CollectionPeriod = 6;

        public SendPaymentsSteps(TestContext testContext)
        {
            _testContext = testContext;
        }

        [Given(@"payments exist for a legal entity")]
        public async Task GivenPaymentsExistForALegalEntity()
        {
            _validatePaymentData = new ValidatePaymentsSteps.ValidatePaymentData(_testContext);
            await _validatePaymentData.Create();                        

            await RunPaymentsProcess();
        }

        [Given(@"clawbacks exist for a legal entity")]
        public async Task GivenClawbacksExistForALegalEntity()
        {
            _validatePaymentData = new ValidatePaymentsSteps.ValidatePaymentData(_testContext);
            _validatePaymentData.AddClawbackPayment(false);
            await _validatePaymentData.Create();            

            await RunPaymentsProcess();
        }

        [When(@"the payments have been approved")]
        public async Task WhenPaymentsAreApproved()
        {
            _testContext.PaymentsApi.MockServer
                .Given(
                    Request
                        .Create()
                        .WithPath($"/payments/requests")
                        .WithHeader("Content-Type", "application/payments-data")
                        .WithParam("api-version", "2020-10-01")
                        .UsingPost()
                )
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.Accepted)
                    .WithHeader("Content-Type", "application/json"));

            await ApprovePayments();
        }

        [When(@"the payments have been rejected")]
        public async Task WhenPaymentsAreRejected()
        {            
            await RejectPayments();
        }

        [Then(@"the payments are sent to Business Central")]
        public async Task ThenThePaymentsAreSent()
        {
            var paymentRequestCount = _testContext.PaymentsApi.MockServer.LogEntries.Count(l => l.RequestMessage.AbsolutePath == "/payments/requests");
            paymentRequestCount.Should().Be(2);

            await using var connection = new SqlConnection(_testContext.SqlDatabase.DatabaseInfo.ConnectionString);
            var payments = await connection.GetAllAsync<Payment>();
            var results = payments
                .Where(x => x.ApprenticeshipIncentiveId == _validatePaymentData.ApprenticeshipIncentiveModel.Id && x.PaymentPeriod <= CollectionPeriod).ToList();
            results.Count.Should().Be(2);
            results.Any(x => !x.PaidDate.HasValue).Should().BeFalse();
        }

        [Then(@"the clawbacks are sent to Business Central")]
        public async Task ThenTheClawbacksAreSent()
        {
            var paymentRequestCount = _testContext.PaymentsApi.MockServer.LogEntries.Count(l => l.RequestMessage.AbsolutePath == "/payments/requests");
            paymentRequestCount.Should().Be(3);

            await using var connection = new SqlConnection(_testContext.SqlDatabase.DatabaseInfo.ConnectionString);
            var clawbacks = await connection.GetAllAsync<ClawbackPayment>();
            var results = clawbacks
                .Where(x => x.ApprenticeshipIncentiveId == _validatePaymentData.ApprenticeshipIncentiveModel.Id && x.CollectionPeriod <= CollectionPeriod).ToList();
            results.Count.Should().Be(1);
            results.Any(x => !x.DateClawbackSent.HasValue).Should().BeFalse();
        }

        [Then(@"the payments are not sent to Business Central")]
        public async Task ThenThePaymentsAreNotSent()
        {
            var paymentRequestCount = _testContext.PaymentsApi.MockServer.LogEntries.Count();
            paymentRequestCount.Should().Be(0);

            await using var connection = new SqlConnection(_testContext.SqlDatabase.DatabaseInfo.ConnectionString);
            var payments = await connection.GetAllAsync<Payment>();
            var results = payments
                .Where(x => x.ApprenticeshipIncentiveId == _validatePaymentData.ApprenticeshipIncentiveModel.Id && x.PaymentPeriod <= CollectionPeriod).ToList();
            results.Count.Should().Be(2);
            results.Any(x => x.PaidDate.HasValue).Should().BeFalse();
        }

        [Then(@"the clawbacks are not sent to Business Central")]
        public async Task ThenTheClawbacksAreNotSent()
        {
            var paymentRequestCount = _testContext.PaymentsApi.MockServer.LogEntries.Count();
            paymentRequestCount.Should().Be(0);

            await using var connection = new SqlConnection(_testContext.SqlDatabase.DatabaseInfo.ConnectionString);
            var clawbacks = await connection.GetAllAsync<ClawbackPayment>();
            var results = clawbacks
                .Where(x => x.ApprenticeshipIncentiveId == _validatePaymentData.ApprenticeshipIncentiveModel.Id && x.CollectionPeriod <= CollectionPeriod).ToList();
            results.Count.Should().Be(1);
            results.Any(x => x.DateClawbackSent.HasValue).Should().BeFalse();
        }

        private async Task RunPaymentsProcess()
        {
            await _testContext.SetActiveCollectionCalendarPeriod(new CollectionPeriod() { Period = CollectionPeriod, Year = CollectionPeriodYear });

            await _testContext.TestFunction.Start(
                new OrchestrationStarterInfo(
                    "IncentivePaymentOrchestrator_HttpStart",
                    nameof(IncentivePaymentOrchestrator),
                    new Dictionary<string, object>
                    {
                        ["req"] = new DummyHttpRequest
                        {
                            Path = $"/api/orchestrators/IncentivePaymentOrchestrator"
                        }
                    },
                    expectedCustomStatus: "WaitingForPaymentApproval"
                ));

            _testContext.TestFunction.LastResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
            var orchestratorStartResponse = await _testContext.TestFunction.GetOrchestratorStartResponse();
            _orchestratorInstanceId = orchestratorStartResponse.Id;
        }      

        private async Task ApprovePayments()
        {
            await _testContext.TestFunction.Start(
                new OrchestrationStarterInfo(
                    "PaymentApproval_HttpStart",
                    nameof(IncentivePaymentOrchestrator),
                    new Dictionary<string, object>
                    {
                        ["req"] = new DummyHttpRequest
                        {
                            Path = $"/api/orchestrators/approvePayments/{_orchestratorInstanceId}"
                        },
                        ["instanceId"] = _orchestratorInstanceId
                    }
                ));

            _testContext.TestFunction.LastResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        private async Task RejectPayments()
        {
            await _testContext.TestFunction.Start(
                new OrchestrationStarterInfo(
                    "PaymentRejection_HttpStart",
                    nameof(IncentivePaymentOrchestrator),
                    new Dictionary<string, object>
                    {
                        ["req"] = new DummyHttpRequest
                        {
                            Path = $"/api/orchestrators/rejectPayments/{_orchestratorInstanceId}"
                        },
                        ["instanceId"] = _orchestratorInstanceId
                    }
                ));

            _testContext.TestFunction.LastResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}