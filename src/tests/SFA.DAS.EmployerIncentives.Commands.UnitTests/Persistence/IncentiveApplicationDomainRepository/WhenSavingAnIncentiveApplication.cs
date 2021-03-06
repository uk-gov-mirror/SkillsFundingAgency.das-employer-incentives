using System.Threading.Tasks;
using AutoFixture;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerIncentives.Abstractions.Events;
using SFA.DAS.EmployerIncentives.Data;
using SFA.DAS.EmployerIncentives.Data.IncentiveApplication;
using SFA.DAS.EmployerIncentives.Domain.Factories;
using SFA.DAS.EmployerIncentives.Domain.IncentiveApplications;
using SFA.DAS.EmployerIncentives.UnitTests.Shared.AutoFixtureCustomizations;

namespace SFA.DAS.EmployerIncentives.Commands.UnitTests.Persistence.IncentiveApplicationDomainRepository
{
    public class WhenSavingAnIncentiveApplication
    {
        private Commands.Persistence.IncentiveApplicationDomainRepository _sut;
        private Mock<IIncentiveApplicationDataRepository> _mockIncentiveApplicationDataRepository;
        private Mock<IIncentiveApplicationFactory> _mockIncentiveApplicationFactory;
        private Mock<IDomainEventDispatcher> _mockDomainEventDispatcher;
        
        private Fixture _fixture;

        [SetUp]
        public void Arrange()
        {
            _fixture = new Fixture();
            _fixture.Customize(new IncentiveApplicationCustomization());
            
            _mockIncentiveApplicationDataRepository = new Mock<IIncentiveApplicationDataRepository>();
            _mockIncentiveApplicationFactory = new Mock<IIncentiveApplicationFactory>();
            _mockDomainEventDispatcher = new Mock<IDomainEventDispatcher>();

            _sut = new Commands.Persistence.IncentiveApplicationDomainRepository(_mockIncentiveApplicationDataRepository.Object, _mockIncentiveApplicationFactory.Object, _mockDomainEventDispatcher.Object);
        }

        [Test]
        public async Task Then_a_new_entity_is_persisted_by_the_data_layer()
        {
            //Arrange
            var entity = _fixture.Create<IncentiveApplication>();

            //Act
            await _sut.Save(entity);

            //Assert
            _mockIncentiveApplicationDataRepository.Verify(m => m.Add(entity.GetModel()), Times.Once);
        }
    }
}
