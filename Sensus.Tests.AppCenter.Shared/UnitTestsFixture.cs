// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Xamarin.UITest;
using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Sensus.Tests.AppCenter.Shared
{
    [TestFixture]   
    public class UnitTestsFixture
    {
        private IApp _app;

        [SetUp]
        public void BeforeEachTest()
        {
            _app = ConfigureApp
#if __IOS__
                   .iOS
#elif __ANDROID__
                   .Android
#endif
                   .StartApp();
        }

        [Test]
        public void UnitTests()
        {
            string resultsStr = _app.WaitForElement(c => c.All().Marked("unit-test-results"), timeout: TimeSpan.FromMinutes(2)).FirstOrDefault()?.Text;
            Assert.NotNull(resultsStr);

            Console.Out.WriteLine("Test results:  " + resultsStr);

            string[] results = resultsStr.Split();
            Assert.AreEqual(results.Length, 1);

            int testsPassed = int.Parse(results[0].Split(':')[1]);

            int totalTests;
#if __IOS__
            totalTests = 154;
#elif __ANDROID__
            totalTests = 155;
#endif

            Assert.AreEqual(testsPassed, totalTests);
        }
    }
}
