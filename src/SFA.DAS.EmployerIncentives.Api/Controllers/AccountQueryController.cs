﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.EmployerIncentives.Queries;
using SFA.DAS.EmployerIncentives.Queries.Account;

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
        public async Task<GetLegalEntitiesResponse> GetLegalEntities(long accountId)
        {
            var request = new GetLegalEntitiesRequest(accountId);

            var res = await QueryAsync<GetLegalEntitiesRequest, GetLegalEntitiesResponse>(request);
            
            return res;
        }
    }
}
