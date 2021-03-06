using SFA.DAS.EmployerIncentives.Abstractions.Commands;

namespace SFA.DAS.EmployerIncentives.Commands.SendEmail
{
    public class SendBankDetailsReminderEmailCommand : ICommand
    {
        public long AccountId { get; private set; }
        public long AccountLegalEntityId { get; private set; }
        public string EmailAddress{ get; private set; }

        public string AddBankDetailsUrl { get; private set; }

        public SendBankDetailsReminderEmailCommand(long accountId, long accountLegalEntityId, string emailAddress, string addBankDetailsUrl)
        {
            AccountId = accountId;
            AccountLegalEntityId = accountLegalEntityId;
            EmailAddress = emailAddress;
            AddBankDetailsUrl = addBankDetailsUrl;
        }
    }
}
