﻿using AutoFixture;
using Moq;
using NUnit.Framework;
using SFA.DAS.EmployerIncentives.Application.Persistence;
using SFA.DAS.EmployerIncentives.Data;
using SFA.DAS.EmployerIncentives.Domain.Entities;
using System.Threading.Tasks;

namespace SFA.DAS.EmployerIncentives.Application.UnitTests.Persistence
{
    public class WhenSavingAnAccountAggregate
    {
        private AccountDomainRepository _sut;
        private Mock<IAccountDataRepository> _mockAccountDataRepository;

        private Fixture _fixture;

        [SetUp]
        public void Arrange()
        {
            _fixture = new Fixture();

            _mockAccountDataRepository = new Mock<IAccountDataRepository>();

            _sut = new AccountDomainRepository(_mockAccountDataRepository.Object);
        }

        [Test]
        public async Task Then_the_entity_is_persisted_by_the_data_layer()
        {
            //Arrange
            var entity = _fixture.Create<Account>();

            //Act
            await _sut.Save(entity);

            //Assert
            Assert.Inconclusive();            
        }
    }
}
