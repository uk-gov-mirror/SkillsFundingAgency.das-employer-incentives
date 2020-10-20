﻿using SFA.DAS.EmployerIncentives.Abstractions.Commands;
using SFA.DAS.EmployerIncentives.Commands.Persistence;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Commands.UpdateVrfCaseStatusForLegalEntity
{
    public class UpdateVendorRegistrationCaseStatusCommandHandler : ICommandHandler<UpdateVendorRegistrationCaseStatusCommand>
    {
        private readonly IAccountDomainRepository _domainRepository;

        public UpdateVendorRegistrationCaseStatusCommandHandler(IAccountDomainRepository domainRepository)
        {
            _domainRepository = domainRepository;
        }

        public async Task Handle(UpdateVendorRegistrationCaseStatusCommand command, CancellationToken cancellationToken = default)
        {
            var accounts = await _domainRepository.GetByHashedLegalEntityId(command.HashedLegalEntityId);

            foreach (var account in accounts)
            {
                account.UpdateVendorRegistrationCaseStatus(command.HashedLegalEntityId, command.CaseId, command.Status, command.CaseStatusLastUpdatedDate);

                await _domainRepository.Save(account);
            }
        }
    }
}
