using SFA.DAS.EmployerIncentives.Abstractions.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Commands.Decorators
{
    public class CommandHandlerWithRetry<T> : ICommandHandler<T> where T : ICommand
    {
        private readonly ICommandHandler<T> _handler;
        private readonly Policies _policies;

        public CommandHandlerWithRetry(
            ICommandHandler<T> handler,
            Policies policies)
        {
            _handler = handler;
            _policies = policies;
        }

        public Task Handle(T command, CancellationToken cancellationToken = default)
        {
            return _policies.LockRetryPolicy.ExecuteAsync((cancellationToken) => _handler.Handle(command, cancellationToken), cancellationToken);
        }
    }
}
