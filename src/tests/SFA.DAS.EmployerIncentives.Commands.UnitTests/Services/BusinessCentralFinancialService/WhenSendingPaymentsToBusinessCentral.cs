﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using SFA.DAS.EmployerIncentives.Abstractions.DTOs.Queries.ApprenticeshipIncentives;
using SFA.DAS.EmployerIncentives.Commands.Services.BusinessCentralApi;
using SFA.DAS.EmployerIncentives.Commands.Services.LearnerMatchApi;
using SFA.DAS.EmployerIncentives.Commands.UnitTests.Persistence.AccountDomainRepository;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives.Models;
using SFA.DAS.EmployerIncentives.Domain.Factories;
using SFA.DAS.EmployerIncentives.Enums;
using SFA.DAS.EmployerIncentives.UnitTests.Shared;

namespace SFA.DAS.EmployerIncentives.Commands.UnitTests.Services.BusinessCentralFinancialService
{
    public class WhenSendingPaymentsToBusinessCentral
    {
        private BusinessCentralFinancePaymentsService _sut;
        private TestHttpClient _httpClient;
        private Uri _baseAddress;
        private readonly string _apiVersion = "1.0";
        private Fixture _fixture;

        [SetUp]
        public void Arrange()
        {
            _fixture = new Fixture();
            _baseAddress = new Uri(@"http://localhost");
            _httpClient = new TestHttpClient(_baseAddress);

            _sut = new BusinessCentralFinancePaymentsService(_httpClient, 3, "XXX");
        }

        [Test]
        public async Task Then_the_payment_is_posted_and_we_get_a_successful_completed_confirmation_response()
        {
            //Arrange
            _httpClient.SetUpPostAsAsync(System.Net.HttpStatusCode.Accepted);
            var payment = _fixture.Create<PaymentDto>();

            //Act
            var response = await _sut.SendPaymentRequestsForLegalEntity(new List<PaymentDto> { payment });

            response.AllPaymentsSent.Should().BeTrue();
            response.PaymentsSent.Count.Should().Be(1);
            response.PaymentsSent[0].Should().Be(payment);
        }

        [Test]
        public async Task Then_the_payment_is_posted_and_we_get_a_successful_but_not_yet_fully_completed_confirmation_response()
        {
            //Arrange
            _httpClient.SetUpPostAsAsync(System.Net.HttpStatusCode.Accepted);
            var payments = _fixture.CreateMany<PaymentDto>(5).ToList();

            //Act
            var response = await _sut.SendPaymentRequestsForLegalEntity(payments);

            response.AllPaymentsSent.Should().BeFalse();
            response.PaymentsSent.Count.Should().Be(3);
            response.PaymentsSent[0].Should().Be(payments[0]);
            response.PaymentsSent[1].Should().Be(payments[1]);
            response.PaymentsSent[2].Should().Be(payments[2]);
        }

        [TestCase(HttpStatusCode.InternalServerError)]
        [TestCase(HttpStatusCode.ServiceUnavailable)]
        [TestCase(HttpStatusCode.BadGateway)]
        public async Task Then_the_payment_is_posted_and_we_get_an_internal_error_from_business_central_api(HttpStatusCode statusCode)
        {
            //Arrange
            _httpClient.SetUpPostAsAsync(statusCode);
            var payment = _fixture.Create<PaymentDto>();

            //Act
            Func<Task> act = async () => await _sut.SendPaymentRequestsForLegalEntity(new List<PaymentDto> { payment });

            act.Should().Throw<BusinessCentralApiException>().WithMessage("Business Central API is unavailable and returned an internal*");
        }

        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.Unauthorized)]
        [TestCase(HttpStatusCode.Forbidden)]
        public async Task Then_the_payment_is_posted_and_we_get_an_access_error_from_business_central_api(HttpStatusCode statusCode)
        {
            //Arrange
            _httpClient.SetUpPostAsAsync(statusCode);
            var payment = _fixture.Create<PaymentDto>();

            //Act
            Func<Task> act = async () => await _sut.SendPaymentRequestsForLegalEntity(new List<PaymentDto> { payment });

            act.Should().Throw<BusinessCentralApiException>().WithMessage("Business Central API returned*");
        }

        [Test]
        public void Then_the_payment_fields_are_mapped_correctly()
        {

            var payment = _fixture.Build<PaymentDto>().Create();

            var paymentRequest = _sut.MapToBusinessCentralPaymentRequest(payment);

            paymentRequest.RequestorUniquePaymentIdentifier.Should().Be(payment.PaymentId);
            paymentRequest.Requestor.Should().Be("ApprenticeServiceEI");
            paymentRequest.FundingStream.Code.Should().Be("EIAPP");
            paymentRequest.FundingStream.StartDate.Should().Be(new DateTime(2020, 9, 1));
            paymentRequest.FundingStream.EndDate.Should().Be(new DateTime(2021, 8, 30));
            paymentRequest.DueDate.Should().Be(payment.DueDate);
            paymentRequest.VendorNo.Should().Be(payment.VendorId);
            paymentRequest.CostCentreCode.Should().Be("AAA40");
            paymentRequest.Amount.Should().Be(payment.Amount);
            paymentRequest.Currency.Should().Be("GBP");
            paymentRequest.ExternalReference.Type.Should().Be("ApprenticeIdentifier");
            paymentRequest.ExternalReference.Value.Should().Be(payment.AccountLegalEntityId.ToString());
        }

        [TestCase(SubnominalCode.Levy16To18, "2240147")]
        [TestCase(SubnominalCode.Levy19Plus, "2340147")]
        [TestCase(SubnominalCode.NonLevy16To18, "2240250")]
        [TestCase(SubnominalCode.NonLevy19Plus, "2340292")]
        public void Then_the_SubnominalCodes_are_mapped_to_accountcode(SubnominalCode subnominalCode, string expectedAccountCode)
        {

            var payment = _fixture.Build<PaymentDto>().With(x => x.SubnominalCode, subnominalCode).Create();

            var paymentRequest = _sut.MapToBusinessCentralPaymentRequest(payment);

            paymentRequest.AccountCode.Should().Be(expectedAccountCode);
        }

        [TestCase("first", "XXX", 12345, "Hire a new apprentice (first payment). Employer: XXX ULN: 12345")]
        [TestCase("second", "XXX", 12345, "Hire a new apprentice (second payment). Employer: XXX ULN: 12345")]
        public void Then_the_PaymentLineDescription_is_constructed_as_expected(string step, string hashedLegalEntityId, long uln, string expected)
        {

            var payment = _fixture.Build<PaymentDto>()
                .With(x => x.PaymentSequence, step)
                .With(x => x.HashedLegalEntityId, hashedLegalEntityId)
                .With(x => x.ULN, uln)
                .Create();

            var paymentRequest = _sut.MapToBusinessCentralPaymentRequest(payment);

            paymentRequest.PaymentLineDescription.Should().Be(expected);
        }
    }
}
