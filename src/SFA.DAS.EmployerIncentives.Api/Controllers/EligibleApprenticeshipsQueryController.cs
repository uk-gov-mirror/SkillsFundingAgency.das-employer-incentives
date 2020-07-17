﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.EmployerIncentives.Abstractions.DTOs;
using SFA.DAS.EmployerIncentives.Abstractions.Queries;
using SFA.DAS.EmployerIncentives.Queries.NewApprenticeIncentive.GetApprenticeshipEligibility;

namespace SFA.DAS.EmployerIncentives.Api.Controllers
{
    [Route("eligibleapprenticeships")]
    [ApiController]
    public class EligibleApprenticeshipsQueryController : ApiQueryControllerBase
    {
        public EligibleApprenticeshipsQueryController(IQueryDispatcher queryDispatcher) : base(queryDispatcher)
        {
        }

        [HttpGet("{uln}")]
        public async Task<IActionResult> IsApprenticeshipEligible(long uln, [FromQuery]DateTime startDate, [FromQuery]bool isApproved)
        {
            var request = new GetApprenticeshipEligibilityRequest(new ApprenticeshipDto { UniqueLearnerNumber = uln, StartDate = startDate, IsApproved = isApproved });
            var response = await QueryAsync<GetApprenticeshipEligibilityRequest, GetApprenticeshipEligibilityResponse>(request);

            if (response.IsEligible)
            {
                return Ok();
            }

            return NotFound();
        }
    }
}
