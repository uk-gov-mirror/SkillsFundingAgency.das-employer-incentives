﻿using System;
using System.ComponentModel.DataAnnotations.Schema;
using SFA.DAS.EmployerIncentives.Enums;

namespace SFA.DAS.EmployerIncentives.Data.ApprenticeshipIncentives.Models.Archive
{
    [Dapper.Contrib.Extensions.Table("archive.Payment")]
    [Table("Payment", Schema = "archive")]
    public partial class Payment
    {
        [Dapper.Contrib.Extensions.ExplicitKey]
        public Guid PaymentId { get; set; }
        public Guid ApprenticeshipIncentiveId { get; set; }
        public Guid PendingPaymentId { get; set; }
        public long AccountId { get; set; }
        public long AccountLegalEntityId { get; set; }
        public decimal Amount { get; set; }
        public DateTime CalculatedDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public SubnominalCode SubnominalCode { get; set; }
        public byte PaymentPeriod { get; set; }
        public short PaymentYear { get; set; }
        public DateTime ArchiveDateUTC { get; set; }
    }
}
