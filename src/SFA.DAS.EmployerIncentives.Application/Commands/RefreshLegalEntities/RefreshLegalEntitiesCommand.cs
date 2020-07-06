﻿namespace SFA.DAS.EmployerIncentives.Application.Commands.RefreshLegalEntities
{
    public class RefreshLegalEntitiesCommand : ICommand
    {
        public int PageNumber { get; private set; }
        public int PageSize { get; private set; }

        public RefreshLegalEntitiesCommand(int pageNumber = 1, int pageSize = 1000)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
