﻿using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Commands
{
    public interface ICommandDispatcher
    {
        Task SendAsync<T>(T command, CancellationToken cancellationToken = default) where T : ICommand;
    }
}
