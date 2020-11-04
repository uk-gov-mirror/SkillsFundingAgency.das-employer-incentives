﻿using System;
using System.IO;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace SFA.DAS.EmployerIncentives.Api.AcceptanceTests.Bindings
{
    [Binding]
    public class TestCleanUp
    {
        private readonly TestContext _context;
        public TestCleanUp(TestContext context)
        {
            _context = context;
        }

        [AfterScenario()]
        public async Task CleanUp()
        {
            if (_context.MessageBus != null && _context.MessageBus.IsRunning)
            {
                await _context.MessageBus.Stop();
            }
            _context.EmployerIncentivesWebApiFactory?.Dispose();
            _context.EmployerIncentiveApi?.Dispose();
            _context.AccountApi?.Dispose();
            _context.DomainMessageHandlers?.Dispose();
            _context.SqlDatabase?.Dispose();
           
            try
            {
                Directory.Delete(_context.TestDirectory.FullName, true);
            }
            catch(Exception){}
        }
    }
}