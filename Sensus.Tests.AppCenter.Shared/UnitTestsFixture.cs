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
using Xamarin.UITest;
using NUnit.Framework;

namespace Sensus.Tests.AppCenter.Shared
{
    [TestFixture]   
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

        [Test]
        public void UnitTests()
        {
            _app.Repl();
        }
    }
}
