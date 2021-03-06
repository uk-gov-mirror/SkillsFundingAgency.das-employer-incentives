namespace SFA.DAS.EmployerIncentives.Abstractions.DTOs.Queries.ApprenticeshipIncentives
{
    public class CollectionPeriodDto
    {
        public byte CollectionPeriodNumber { get; set; }
        public short CollectionYear { get; set; }

        public override string ToString()
        {
            return CollectionYear + " - " + CollectionPeriodNumber;
        }
    }
}
