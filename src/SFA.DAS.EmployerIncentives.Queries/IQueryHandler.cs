﻿using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Queries
{
    public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery
    {
        Task<TResult> Handle(TQuery query, CancellationToken cancellationToken = default);
    }
}
