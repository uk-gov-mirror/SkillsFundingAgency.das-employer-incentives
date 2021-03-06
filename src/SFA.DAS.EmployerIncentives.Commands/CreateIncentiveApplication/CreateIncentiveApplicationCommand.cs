using SFA.DAS.EmployerIncentives.Abstractions.Commands;
using SFA.DAS.EmployerIncentives.Abstractions.DTOs.Commands;
using System;
using System.Collections.Generic;

namespace SFA.DAS.EmployerIncentives.Commands.CreateIncentiveApplication
{
    public class CreateIncentiveApplicationCommand : ICommand
    {
        public Guid IncentiveApplicationId { get; }
        public long AccountId { get; }
        public long AccountLegalEntityId { get; }
        public IEnumerable<IncentiveApplicationApprenticeshipDto> Apprenticeships { get; }

        public CreateIncentiveApplicationCommand(
            Guid incentiveApplicationId,
            long accountId,
            long accountLegalEntityId,
            IEnumerable<IncentiveApplicationApprenticeshipDto> apprenticeships)
        {
            IncentiveApplicationId = incentiveApplicationId;
            AccountId = accountId;
            AccountLegalEntityId = accountLegalEntityId;
            Apprenticeships = apprenticeships;
        }
    }
}
