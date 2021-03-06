using Microsoft.Azure.WebJobs;
using SFA.DAS.EmployerIncentives.Commands.Types.ApprenticeshipIncentive;
using SFA.DAS.EmployerIncentives.Infrastructure;
using SFA.DAS.NServiceBus.AzureFunction.Attributes;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Functions.DomainMessageHandlers
{
    public class HandleApprenticeshipIncentivesCreateCommand
    {
        private readonly ICommandService _commandService;

        public HandleApprenticeshipIncentivesCreateCommand(ICommandService commandService)
        {
            _commandService = commandService;
        }

        [FunctionName(nameof(HandleApprenticeshipIncentivesCreateCommand))]
        public async Task HandleCommand([NServiceBusTrigger(Endpoint = QueueNames.ApprenticeshipIncentivesCreate)] CreateIncentiveCommand command)
        {
            await _commandService.Dispatch(command);
        }
    }
}
