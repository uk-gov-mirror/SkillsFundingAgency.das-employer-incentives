using SFA.DAS.EmployerIncentives.Abstractions.Commands;
using SFA.DAS.EmployerIncentives.Commands.Exceptions;
using SFA.DAS.EmployerIncentives.Infrastructure.DistributedLock;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Commands.Decorators
{
    public class CommandHandlerWithDistributedLock<T> : ICommandHandler<T> where T : ICommand
    {
        private readonly ICommandHandler<T> _handler;
        private readonly IDistributedLockProvider _lockProvider;

        public CommandHandlerWithDistributedLock(
            ICommandHandler<T> handler,
            IDistributedLockProvider lockProvider)
        {
            _handler = handler;
            _lockProvider = lockProvider;
        }

        public async Task Handle(T command, CancellationToken cancellationToken = default)
        {
            if (command is ILockIdentifier identifier)
            {
                await _lockProvider.Start();

                try
                {
                    var lockId = identifier.LockId;
                    try
                    {
                        if (!await _lockProvider.AcquireLock(lockId, cancellationToken))
                        {
                            throw new EntityLockedException($"Unable to handle command '{command.GetType().FullName}'. The entity with with identifier '{lockId}' is already handling a command.");
                        }

                        await _handler.Handle(command, cancellationToken);
                    }
                    finally
                    {
                        await _lockProvider.ReleaseLock(lockId);
                    }
                }
                finally
                {
                    await _lockProvider.Stop();
                }
            }
            else
            {
                await _handler.Handle(command, cancellationToken);
            }
        }
    }
}
