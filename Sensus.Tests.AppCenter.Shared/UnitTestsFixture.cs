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

using System;
using System.Linq;
using Xunit;
using Xamarin.UITest;

namespace Sensus.Tests.AppCenter.Shared
{
    
    public class UnitTestsFixture
    {
        private IApp _app;

        [SetUp]
        public void BeforeEachTest()
        {
            // TODO: If the iOS app being tested is included in the solution then open
            // the Unit Tests window, right click Test Apps, select Add App Project
            // and select the app projects that should be tested.
            //
            // The iOS project should have the Xamarin.TestCloud.Agent NuGet package
            // installed. To start the Test Cloud Agent the following code should be
            // added to the FinishedLaunching method of the AppDelegate:
            //
            //    #if ENABLE_TEST_CLOUD
            //    Xamarin.Calabash.Start();
            //    #endif
            _app = ConfigureApp
#if __IOS__
                   .iOS
#elif __ANDROID__
                   .Android
#endif
                   // TODO: Update this path to point to your iOS app and uncomment the
                   //code if the app is not included in the solution.
                   //.AppBundle ("../../../iOS/bin/iPhoneSimulator/Debug/Sensus.iOS.Tests.AppCenter.iOS.app")
                   .StartApp();
        }

        [Fact]
        public void UnitTests()
        {
            string log = _app.WaitForElement(c => c.All().Marked("sensus-test-log"), timeout: TimeSpan.FromMinutes(2)).FirstOrDefault()?.Text;
            Assert.NotNull(log);
            Console.Out.WriteLine(log);

            string[] logLines = log.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

#if __IOS__
            string[] resultParts = logLines.Last().Split(':');
            Assert.Equal(resultParts.Length, 6);

            int testsRun = int.Parse(resultParts[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0]);
            int testsPassed = int.Parse(resultParts[2].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0]);
            int testsFailed = int.Parse(resultParts[4].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0]);
            int testsSkipped = int.Parse(resultParts[5].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0]);
            int testsInconclusive = int.Parse(resultParts[3].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[0]);

            Assert.True(testsRun == 154);  // will need to update this as we develop. ensures that tests are actually run.
#elif __ANDROID__
            string[] resultParts = logLines.Last().Split(',');
            Assert.Equal(resultParts.Length, 5);

            int testsRun = int.Parse(resultParts[0].Split(':')[1]);
            int testsPassed = int.Parse(resultParts[1].Split(':')[1]);
            int testsFailed = int.Parse(resultParts[2].Split(':')[1]);
            int testsSkipped = int.Parse(resultParts[3].Split(':')[1]);
            int testsInconclusive = int.Parse(resultParts[4].Split(':')[1]);

            Assert.True(testsRun == 153);  // will need to update this as we develop. ensures that tests are actually run.
#endif
            Assert.Equal(testsRun, testsPassed);
            Assert.Equal(testsFailed, 0);
            Assert.Equal(testsSkipped, 0);
            Assert.Equal(testsInconclusive, 0);
        }
    }
}
