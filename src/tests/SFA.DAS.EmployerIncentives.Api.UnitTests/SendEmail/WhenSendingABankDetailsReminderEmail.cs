using AutoFixture;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerIncentives.Abstractions.Commands;
using SFA.DAS.EmployerIncentives.Api.Controllers;
using SFA.DAS.EmployerIncentives.Api.Types;
using SFA.DAS.EmployerIncentives.Commands.SendEmail;
using System.Threading;

namespace SFA.DAS.EmployerIncentives.Api.UnitTests.SendEmail
{
    [TestFixture]
    public class WhenSendingABankDetailsReminderEmail
    {
        private EmailCommandController _sut;
        private Mock<ICommandDispatcher> _commandDispatcher;
        private Fixture _fixture;

        [SetUp]
        public void Arrange()
        {
            _commandDispatcher = new Mock<ICommandDispatcher>();
            _fixture = new Fixture();
            _sut = new EmailCommandController(_commandDispatcher.Object);
        }

        [Test]
        public void Then_the_send_email_command_is_published()
        {
            // Arrange
            var request = _fixture.Create<SendBankDetailsEmailRequest>();

            // Act
            var result = _sut.SendBankDetailRequiredEmail(request);

            // Assert
            result.Should().NotBeNull();
            _commandDispatcher.Verify(x => x.Send(It.Is<SendBankDetailsRequiredEmailCommand>(cmd => cmd.AccountId == request.AccountId &&
                                                                                             cmd.AccountLegalEntityId == request.AccountLegalEntityId &&
                                                                                             cmd.AddBankDetailsUrl == request.AddBankDetailsUrl &&
                                                                                             cmd.EmailAddress == request.EmailAddress),
                                                                                             It.IsAny<CancellationToken>()));
        }
    }
}
