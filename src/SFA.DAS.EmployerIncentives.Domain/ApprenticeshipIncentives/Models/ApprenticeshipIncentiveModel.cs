﻿using SFA.DAS.EmployerIncentives.Abstractions.Domain;
using SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives.ValueTypes;
using System;
using System.Collections.Generic;

namespace SFA.DAS.EmployerIncentives.Domain.ApprenticeshipIncentives.Models
{
    public class ApprenticeshipIncentiveModel : IEntityModel<Guid>
    {
        public Guid Id { get; set; }
        public Account Account { get; set; }        
        public Apprenticeship Apprenticeship { get; set; }
        public ICollection<PendingPaymentModel> PendingPaymentModels { get; set; }

        public ApprenticeshipIncentiveModel()
        {
            PendingPaymentModels = new List<PendingPaymentModel>();
        }
    }
}