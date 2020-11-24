﻿using SFA.DAS.EmployerIncentives.Functions.PaymentsProcess.AcceptanceTests.Services;
using System;
using System.Diagnostics;
using TechTalk.SpecFlow;

namespace SFA.DAS.EmployerIncentives.Functions.PaymentsProcess.AcceptanceTests.Bindings
{
    [Binding]
    public class LearnerMatchApiPerTestRunHook
    {
        private readonly TestContext _testContext;

        public LearnerMatchApiPerTestRunHook(TestContext testContext)
        {
            _testContext = testContext;
        }

        [BeforeScenario(Order = 3)]
        public void InitialiseLearnerMatchApi()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _testContext.LearnerMatchApi = new TestLearnerMatchApi();
            stopwatch.Stop();
            Console.WriteLine($"[{nameof(LearnerMatchApiPerTestRunHook)}] time it took to spin up LearnerMatchApi: {stopwatch.Elapsed.Milliseconds} milliseconds");
        }

        [AfterScenario()]
        public void CleanUpLearnerMatchApi()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            _testContext.LearnerMatchApi?.Dispose();
            Console.WriteLine($"[{nameof(LearnerMatchApiPerTestRunHook)}] time it took to dispose of LearnerMatchApi: {stopwatch.Elapsed.Milliseconds} milliseconds");
        }
    }
}
