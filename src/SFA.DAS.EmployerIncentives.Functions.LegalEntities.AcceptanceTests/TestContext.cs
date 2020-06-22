﻿using AutoFixture;
using Microsoft.Extensions.Hosting;
using SFA.DAS.EmployerIncentives.Data.UnitTests.TestHelpers;
using SFA.DAS.EmployerIncentives.Functions.TestConsole;
using System;
using System.IO;

namespace SFA.DAS.EmployerIncentives.Functions.LegalEntities.AcceptanceTests
{
    public class TestContext
    {
        public DirectoryInfo TestDirectory { get; set; }

        public TestData TestData {get; set;}

        public SqlHelper.DatabaseProperties DatabaseProperties { get; set; }

        public TestMessageBus TestMessageBus { get; set; }

        public IServiceProvider ServiceProvider { get; set; }

        public IHost FunctionsHost { get; set; }

        public Fixture Fixture { get; set; }

        public TestContext()
        {
            TestDirectory = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString()));
            if(!TestDirectory.Exists)
            {
                Directory.CreateDirectory(TestDirectory.FullName);
            }
        }
    }
}
