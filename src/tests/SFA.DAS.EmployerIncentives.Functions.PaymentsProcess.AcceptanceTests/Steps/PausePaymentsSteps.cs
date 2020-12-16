﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using FluentAssertions;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;
using Newtonsoft.Json;
using SFA.DAS.EmployerIncentives.Data.ApprenticeshipIncentives.Models;
using SFA.DAS.EmployerIncentives.Data.Models;
using SFA.DAS.EmployerIncentives.Enums;
using SFA.DAS.EmployerIncentives.Functions.TestHelpers;
using TechTalk.SpecFlow;

namespace SFA.DAS.EmployerIncentives.Functions.PaymentsProcess.AcceptanceTests.Steps
{
    [Binding]
    [Scope(Feature = "PausePayments")]
    public partial class PausePaymentsSteps
    {
        private readonly TestContext _testContext;

        private long _accountLegalEntityId;
        private PausePaymentsRequest _request;
        private ValidatePaymentsSteps.ValidatePaymentData _paymentData;

        public PausePaymentsSteps(TestContext testContext)
        {
            _testContext = testContext;
        }

        [Given(@"apprenticeship incentive does not exist")]
        public void GivenApprenticeshipIncentiveDoesNotExist()
        {
            _accountLegalEntityId = 3453434;
            _request = CreatePausePaymentsRequest(6566, PausePaymentsAction.Pause);
        }

        [Given(@"an apprenticeship incentive exists")]
        public async Task GivenApprenticeshipIncentiveExists()
        {
            _paymentData = new ValidatePaymentsSteps.ValidatePaymentData(_testContext);
            _request = CreatePausePaymentsRequest(_paymentData.ApprenticeshipIncentiveModel.ULN, PausePaymentsAction.Pause);
            _accountLegalEntityId = _paymentData.AccountModel.AccountLegalEntityId;
            await _paymentData.Create();
        }

        [Given(@"a paused apprenticeship incentive exists")]
        public async Task GivenAPausedApprenticeshipIncentiveExists()
        {
            _paymentData = new ValidatePaymentsSteps.ValidatePaymentData(_testContext);
            _request = CreatePausePaymentsRequest(_paymentData.ApprenticeshipIncentiveModel.ULN, PausePaymentsAction.Pause);
            _accountLegalEntityId = _paymentData.AccountModel.AccountLegalEntityId;
            _paymentData.ApprenticeshipIncentiveModel.PausePayments = true;
            await _paymentData.Create();
        }

        [When(@"the pause payments request is sent")]
        public async Task WhenThePausePaymentsRequestIsSent()
        {
            await _testContext.TestFunction.CallEndpoint(
                new EndpointInfo(
                    "PausePaymentsRequest",
                    new Dictionary<string, object>
                    {
                        ["req"] = new DummyHttpRequest(JsonConvert.SerializeObject(_request))
                        {
                            Path = $"/accountlegalentity/{_accountLegalEntityId}/payments",
                            Method = "patch"
                        },
                        ["accountLegalEntityId"] = _accountLegalEntityId
                    })
                );
        }

        [Then(@"the requester is informed no apprenticeship incentive is found")]
        public void ThenTheRequesterIsInformedNoApprenticeshipIncentiveIsFound()
        {
            _testContext.TestFunction.HttpObjectResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        [Then(@"the requester is informed the apprenticeship incentive is paused")]
        public void ThenTheRequesterIsInformedTheApprenticeshipIncentiveIsPaused()
        {
            _testContext.TestFunction.HttpObjectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
            var content = _testContext.TestFunction.HttpObjectResult.Value;
            JsonConvert.SerializeObject(content).Should().Contain("Payments have been successfully Paused");
        }

        [Then(@"the requester is informed the apprenticeship incentive is already paused")]
        public void ThenTheRequesterIsInformedTheApprenticeshipIncentiveIsAlreadyPaused()
        {
            _testContext.TestFunction.HttpObjectResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            var content = _testContext.TestFunction.HttpObjectResult.Value;
            JsonConvert.SerializeObject(content).Should().Contain("Payments already paused");
        }

        [Then(@"the PausePayment status is set to true")]
        public void ThenThePausePaymentStatusIsSetToTrue()
        {
            using var dbConnection = new SqlConnection(_testContext.SqlDatabase.DatabaseInfo.ConnectionString);
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
            statusAudit.IncentiveApplicationApprenticeshipId.Should().Be(_paymentData.ApprenticeshipIncentiveModel.IncentiveApplicationApprenticeshipId);
            statusAudit.ServiceRequestCreatedDate.Should().Be(_request.DateServiceRequestTaskCreated);
            statusAudit.ServiceRequestDecisionReference.Should().Be(_request.DecisionReferenceNumber);
            statusAudit.ServiceRequestTaskId.Should().Be(_request.ServiceRequestId);
        }

        private PausePaymentsRequest CreatePausePaymentsRequest(long uln, PausePaymentsAction action)
        {
            return new PausePaymentsRequest
            {
                Action = action,
                ULN = uln,
                DateServiceRequestTaskCreated = DateTime.UtcNow,
                DecisionReferenceNumber = "DecisionReferenceNumber",
                ServiceRequestId = "ServiceRequestId"
            };
        }
    }
}