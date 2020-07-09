﻿using SFA.DAS.EmployerIncentives.Abstractions.Commands;
using SFA.DAS.EmployerIncentives.Commands.Persistence;
using SFA.DAS.EmployerIncentives.Domain.Accounts;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Commands.AddLegalEntity
{
    public class AddLegalEntityCommandHandler : ICommandHandler<AddLegalEntityCommand>
    {
        private readonly IAccountDomainRepository _domainRepository;

        public AddLegalEntityCommandHandler(IAccountDomainRepository domainRepository)
        {
            _domainRepository = domainRepository;
        }

        public async Task Handle(AddLegalEntityCommand command, CancellationToken cancellationToken = default)
        {
            var account = await _domainRepository.Find(command.AccountId);
            if (account != null) 
            {
                if (account.GetLegalEntity(command.AccountLegalEntityId) != null)
                {
                    return; // already created
                }
            }
            else
            {
                account = Account.New(command.AccountId);
            }

            account.AddLegalEntity(command.AccountLegalEntityId, LegalEntity.New(command.LegalEntityId, command.Name));

            await _domainRepository.Save(account);
        }
    }
}