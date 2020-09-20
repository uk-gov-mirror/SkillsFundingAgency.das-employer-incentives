﻿using SFA.DAS.EmployerIncentives.Abstractions.Commands;
using SFA.DAS.EmployerIncentives.Abstractions.Logging;
using SFA.DAS.EmployerIncentives.Domain.Accounts;
using SFA.DAS.EmployerIncentives.Infrastructure.DistributedLock;
using System;

namespace SFA.DAS.EmployerIncentives.Commands.Types.ApprenticeshipIncentive
{
    public class CalculateEarningsCommand : ICommand, ILockIdentifier, ILogWriter
    {
        public Guid ApprenticeshipIncentiveId { get; private set; }
        public long AccountId { get; private set; }
        public long ApprenticeshipId { get; private set; }

        public string LockId { get => $"{nameof(Account)}_{AccountId}"; }

        public CalculateEarningsCommand(
            Guid apprenticeshipIncentiveId,
            long accountId,            
            long apprenticeshipId)
        {
            ApprenticeshipIncentiveId = apprenticeshipIncentiveId;
            AccountId = accountId;
            ApprenticeshipId = apprenticeshipId;
        }

        [Newtonsoft.Json.JsonIgnore]
        public Log Log
        {
            get
            {
                var message = $"ApprenticeshipIncentive CalculateEarningsCommand for AccountId {AccountId}, ApprenticeshipIncentiveId {ApprenticeshipIncentiveId} and ApprenticeshipId {ApprenticeshipId}";
                return new Log
                {
                    OnProcessing = () => message,
                    OnError = () => message
                };
            }
        }
    }
}