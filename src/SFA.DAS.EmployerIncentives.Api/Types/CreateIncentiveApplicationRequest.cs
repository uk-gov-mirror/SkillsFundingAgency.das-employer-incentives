using SFA.DAS.EmployerIncentives.Abstractions.DTOs.Commands;
using System;
using System.Collections.Generic;

namespace SFA.DAS.EmployerIncentives.Api.Types
{
    public class CreateIncentiveApplicationRequest
    {
        public Guid IncentiveApplicationId { get; set; }
        public long AccountId { get; set; }
        public long AccountLegalEntityId { get; set; }
        public IEnumerable<IncentiveApplicationApprenticeshipDto> Apprenticeships { get; set; }
    }
}
