//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Xamarin.UITest;
using NUnit.Framework;
using System;
using System.Linq;

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
            // wait for the tests to complete
            TimeSpan timeout = TimeSpan.FromMinutes(10);

#if __ANDROID__
            string resultsStr = _app.WaitForElement(c => c.All().Marked("unit-test-results"), timeout: timeout).FirstOrDefault()?.Text;
#elif __IOS__
            _app.WaitForElement(c => c.ClassFull("UILabel").Text("Results"), timeout: timeout);
            string resultsStr = _app.Query(c => c.ClassFull("UILabel")).SingleOrDefault(c => c.Text.StartsWith("Passed"))?.Text;
#endif

            Assert.NotNull(resultsStr);

            Console.Out.WriteLine("Test results:  " + resultsStr);

            string[] results = resultsStr.Split();
            Assert.AreEqual(results.Length, 1);

            int testsPassed = int.Parse(results[0].Split(':')[1]);

            int totalTests;
#if __IOS__
            totalTests = 138;
#elif __ANDROID__
            totalTests = 139;
#endif

            Assert.AreEqual(testsPassed, totalTests);
        }
    }
}
