using Microsoft.Azure.WebJobs;
using SFA.DAS.EmployerAccounts.Messages.Events;
using SFA.DAS.EmployerIncentives.Abstractions.Commands;
using SFA.DAS.EmployerIncentives.Commands.RemoveLegalEntity;
using SFA.DAS.EmployerIncentives.Infrastructure;
using SFA.DAS.NServiceBus.AzureFunction.Attributes;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Functions.LegalEntities
{
    public class HandleRemoveLegalEntityEvent
    {
        private readonly ICommandHandler<RemoveLegalEntityCommand> _handler;

        public HandleRemoveLegalEntityEvent(ICommandHandler<RemoveLegalEntityCommand> handler)
        {
            _handler = handler;
        }

        [FunctionName("HandleRemovedLegalEntityEvent")]
        public Task Run([NServiceBusTrigger(Endpoint = QueueNames.RemovedLegalEntity)] RemovedLegalEntityEvent message)
        {
            return _handler.Handle(new RemoveLegalEntityCommand(
                message.AccountId,
                message.AccountLegalEntityId));
        }
    }
}