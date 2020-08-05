﻿using System;
using SFA.DAS.Common.Domain.Types;

namespace SFA.DAS.EmployerIncentives.Abstractions.DTOs.Commands
{
    public class IncentiveApplicationApprenticeshipDto
    {
        public Guid Id { get; set; }
        public int ApprenticeshipId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public long Uln { get; set; }
        public DateTime PlannedStartDate { get; set; }
        public ApprenticeshipEmployerType ApprenticeshipEmployerTypeOnApproval { get; set; }
    }
}