using SFA.DAS.EmployerIncentives.Abstractions.Commands;
using SFA.DAS.EmployerIncentives.Abstractions.DTOs.Queries.ApprenticeshipIncentives;
using SFA.DAS.EmployerIncentives.Commands.Services.BusinessCentralApi;
using SFA.DAS.EmployerIncentives.Data;
using SFA.DAS.EmployerIncentives.Data.ApprenticeshipIncentives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Commands.ApprenticeshipIncentive.SendClawbacks
{
    public class SendClawbacksCommandHandler : ICommandHandler<SendClawbacksCommand>
    {
        private readonly IAccountDataRepository _accountRepository;
        private readonly IPaymentsQueryRepository _queryRepository;
        private readonly IBusinessCentralFinancePaymentsService _businessCentralFinancePaymentsService;

        public SendClawbacksCommandHandler(
            IAccountDataRepository accountRepository,
            IPaymentsQueryRepository queryRepository,
            IBusinessCentralFinancePaymentsService businessCentralFinancePaymentsService)
        {
            _accountRepository = accountRepository;
            _queryRepository = queryRepository;
            _businessCentralFinancePaymentsService = businessCentralFinancePaymentsService;
        }

        public async Task Handle(SendClawbacksCommand command, CancellationToken cancellationToken = default)
        {
            var clawbacks = await _queryRepository.GetUnpaidClawbacks(command.AccountLegalEntityId);
            if (!clawbacks.Any())
            {
                return;
            }

            await Send(clawbacks, command.AccountLegalEntityId, command.ClawbackDate);
        }

        private async Task Send(List<PaymentDto> clawbacks, long accountLegalEntityId, DateTime clawbackDate, CancellationToken cancellationToken = default)
        {
            var clawbacksToSend = clawbacks.Take(_businessCentralFinancePaymentsService.PaymentRequestsLimit).ToList();
            if (!clawbacksToSend.Any())
            {
                return;
            }

            await _businessCentralFinancePaymentsService.SendPaymentRequests(clawbacksToSend);

            await _accountRepository.UpdateClawbackDateForClawbackIds(clawbacksToSend.Select(s => s.PaymentId).ToList(), accountLegalEntityId, clawbackDate);

            if (clawbacks.Count > clawbacksToSend.Count)
            {
                await Send(clawbacks.Skip(_businessCentralFinancePaymentsService.PaymentRequestsLimit).ToList(), accountLegalEntityId, clawbackDate, cancellationToken);
            }
        }
    }
}
