﻿using System;
using SFA.DAS.EmployerIncentives.Abstractions.Commands;
using SFA.DAS.EmployerIncentives.Abstractions.Logging;
using SFA.DAS.EmployerIncentives.Infrastructure.DistributedLock;

namespace SFA.DAS.EmployerIncentives.Commands.ApprenticeshipIncentive.CreatePayment
{
    public class CreatePaymentCommand : DomainCommand, ILockIdentifier, ILogWriter
    {
        public Guid ApprenticeshipIncentiveId { get; }
        public Guid PendingPaymentId { get; }
        public short CollectionYear { get; }
        public Byte CollectionMonth { get; }
        public string LockId { get => $"{nameof(ApprenticeshipIncentiveId)}_{ApprenticeshipIncentiveId}"; }

        public CreatePaymentCommand(Guid apprenticeshipIncentiveId, Guid pendingPaymentId, short collectionYear, byte collectionMonth)
        {
            ApprenticeshipIncentiveId = apprenticeshipIncentiveId;
            PendingPaymentId = pendingPaymentId;
            CollectionYear = collectionYear;
            CollectionMonth = collectionMonth;
        }

        [Newtonsoft.Json.JsonIgnore]
        public Log Log
        {
            get
            {
                var message = $"IncentiveApplications CreatePaymentCommand for ApprenticeshipIncentiveId {ApprenticeshipIncentiveId}, PendingPaymentId {PendingPaymentId}, CollectionYear {CollectionYear} and CollectionMonth {CollectionMonth}";
                return new Log
                {
                    OnProcessing = () => message,
                    OnError = () => message
                };
            }
        }
    }
}
