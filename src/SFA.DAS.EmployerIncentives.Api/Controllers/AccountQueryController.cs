﻿using Microsoft.AspNetCore.Mvc;
using SFA.DAS.EmployerIncentives.Abstractions.Queries;
using System.Linq;
using System.Threading.Tasks;
using SFA.DAS.EmployerIncentives.Queries.Account.GetLegalEntities;
using SFA.DAS.EmployerIncentives.Queries.Account.GetLegalEntity;
using SFA.DAS.EmployerIncentives.Queries.Account.GetApplications;
using SFA.DAS.EmployerIncentives.Queries.Account.GetVendorId;
using SFA.DAS.HashingService;

namespace SFA.DAS.EmployerIncentives.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountQueryController : ApiQueryControllerBase
    {
        public AccountQueryController(IQueryDispatcher queryDispatcher) : base(queryDispatcher)
        {
            
        }

        [HttpGet("/accounts/{accountId}/LegalEntities")]
        public async Task<IActionResult> GetLegalEntities(long accountId)
        {
            var request = new GetLegalEntitiesRequest(accountId);
            var response = await QueryAsync<GetLegalEntitiesRequest, GetLegalEntitiesResponse>(request);

            if(response?.LegalEntities?.Count() > 0)
            {
                return Ok(response.LegalEntities);
            }

            return NotFound();
        }

        [HttpGet("/accounts/{accountId}/LegalEntities/{accountLegalEntityId}")]
        public async Task<IActionResult> GetLegalEntity(long accountId, long accountLegalEntityId)
        {
            var request = new GetLegalEntityRequest(accountId, accountLegalEntityId);
            var response = await QueryAsync<GetLegalEntityRequest, GetLegalEntityResponse>(request);

            if (response?.LegalEntity != null)
            {
                return Ok(response.LegalEntity);
            }

            return NotFound();
        }

        [HttpGet("/accounts/{accountId}/applications")]
        public async Task<IActionResult> GetApplications(long accountId)
        {
            var request = new GetApplicationsRequest(accountId);
            var response = await QueryAsync<GetApplicationsRequest, GetApplicationsResponse>(request);

            if (response?.ApprenticeApplications != null)
            {
                return Ok(response.ApprenticeApplications);
            }

            return NotFound();
        }

        [HttpGet("/legalentities/{hashedLegalEntityId}/employervendorid")]
        public async Task<IActionResult> GetEmployerVendorId(string hashedLegalEntityId)
        {
            var request = new GetVendorIdRequest(hashedLegalEntityId);
            var response = await QueryAsync<GetVendorIdRequest, GetVendorIdResponse>(request);

            if (response?.VendorId == null)
            {
                return NotFound();
            }

            return Ok(response.VendorId);
        }
    }
}
