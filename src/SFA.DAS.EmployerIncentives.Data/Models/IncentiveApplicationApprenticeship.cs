﻿using System;
using System.ComponentModel.DataAnnotations.Schema;
using SFA.DAS.Common.Domain.Types;

namespace SFA.DAS.EmployerIncentives.Data.Models
{
    [Table("IncentiveApplicationApprenticeship")]
    public partial class IncentiveApplicationApprenticeship
    {
        public Guid Id { get; set; }
        public Guid IncentiveApplicationId { get; set; }
        public long ApprenticeshipId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public long Uln { get; set; }
        public DateTime PlannedStartDate { get; set; }
        public ApprenticeshipEmployerType ApprenticeshipEmployerTypeOnApproval { get; set; }
        public decimal TotalIncentiveAmount { get; set; }
        public bool EarningsCalculated { get; set; }
    }
}
