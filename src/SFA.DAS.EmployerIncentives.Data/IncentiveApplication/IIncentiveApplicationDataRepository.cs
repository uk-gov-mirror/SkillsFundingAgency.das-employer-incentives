﻿using System;
using System.Threading.Tasks;
using SFA.DAS.EmployerIncentives.Domain.IncentiveApplications.Models;

namespace SFA.DAS.EmployerIncentives.Data.IncentiveApplication
{
    public interface IIncentiveApplicationDataRepository
    {
        Task Add(IncentiveApplicationModel incentiveApplication);
        Task<IncentiveApplicationModel> Get(Guid incentiveApplicationId);
        Task Update(IncentiveApplicationModel incentiveApplication);
    }
}