﻿using SFA.DAS.EmployerIncentives.Abstractions.Commands;
using SFA.DAS.EmployerIncentives.Domain.Accounts;
using SFA.DAS.EmployerIncentives.Infrastructure.DistributedLock;
using System;

namespace SFA.DAS.EmployerIncentives.Commands.Types.Application
{
    public class CalculateClaimCommand : ICommand, ILockIdentifier
    {
        public long AccountId { get; private set; }
        public Guid IncentiveClaimApplicationId { get; private set; }
        public string LockId { get => $"{nameof(Account)}_{AccountId}"; }

        public CalculateClaimCommand(
            long accountId,
            Guid incentiveClaimApplicationId)
        {
            AccountId = accountId;
            IncentiveClaimApplicationId = incentiveClaimApplicationId;
        }
    }
}
