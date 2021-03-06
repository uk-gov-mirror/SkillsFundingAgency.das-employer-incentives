using System;

namespace SFA.DAS.EmployerIncentives.Abstractions.DTOs.Queries.ApprenticeshipIncentives
{
    public class PendingPaymentDto
    {
        public Guid Id { get; set; }
        public byte? PeriodNumber { get; set; }
        public short? PaymentYear { get; set; }
        public long AccountLegalEntityId { get; set; }
        public DateTime? PaymentMadeDate { get; set; }
        public Guid ApprenticeshipIncentiveId { get; set; }

    }
}
