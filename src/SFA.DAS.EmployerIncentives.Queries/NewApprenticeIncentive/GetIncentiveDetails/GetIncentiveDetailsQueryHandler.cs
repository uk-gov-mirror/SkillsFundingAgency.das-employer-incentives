﻿using System.Threading;
using System.Threading.Tasks;
using SFA.DAS.EmployerIncentives.Abstractions.Queries;

namespace SFA.DAS.EmployerIncentives.Queries.NewApprenticeIncentive.GetIncentiveDetails
{
    public class GetIncentiveDetailsQueryHandler : IQueryHandler<GetIncentiveDetailsRequest, GetIncentiveDetailsResponse>
    {
        public Task<GetIncentiveDetailsResponse> Handle(GetIncentiveDetailsRequest query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GetIncentiveDetailsResponse(ValueObjects.NewApprenticeIncentive.EligibilityStartDate, ValueObjects.NewApprenticeIncentive.EligibilityEndDate));
        }
    }
}