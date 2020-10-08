﻿using System.Collections.Generic;
using System.Threading.Tasks;
using SFA.DAS.EmployerIncentives.Abstractions.DTOs.Queries.ApprenticeshipIncentives;

namespace SFA.DAS.EmployerIncentives.Data.ApprenticeshipIncentives
{
    public interface IPaymentLegalEntityQueryRepository
    {
        Task<List<PayableLegalEntityDto>> GetList(int collectionPeriodYear, int collectionPeriodMonth);
    }
}
