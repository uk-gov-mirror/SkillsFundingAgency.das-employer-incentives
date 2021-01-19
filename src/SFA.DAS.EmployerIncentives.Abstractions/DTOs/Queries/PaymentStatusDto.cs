﻿using System;

namespace SFA.DAS.EmployerIncentives.Abstractions.DTOs.Queries
{
    public class PaymentStatusDto
    {
        public decimal? PaymentAmount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public bool LearnerMatchNotFound { get; set; }
    }
}