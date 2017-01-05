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
using System.Collections.Concurrent;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.iOS;
using Xamarin.UITest.Android;

namespace Sensus.Tests.Cloud
{
    [TestFixture]
    public class Tests
    {
        #region Fields
        ConcurrentBag<IApp> apps;
        #endregion

        #region Setup
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            apps = new ConcurrentBag<IApp>();

            if (TestEnvironment.Platform == TestPlatform.Local)
            {
                //StartApp() doesn't appear to be Thread Safe so we have to start them one at a time.
                //var task1 = Task.Run(() => apps.Add(ConfigureApp.Android.StartApp()));
                //var task2 = Task.Run(() => apps.Add(ConfigureApp.iOS.StartApp()));

                apps.Add(ConfigureApp.Android.StartApp());
                apps.Add(ConfigureApp.iOS.StartApp());
            }

            if (TestEnvironment.Platform == TestPlatform.TestCloudAndroid)
            {
                apps.Add(ConfigureApp.Android.StartApp());
            }

            if (TestEnvironment.Platform == TestPlatform.TestCloudiOS)
            {
                apps.Add(ConfigureApp.iOS.StartApp());
            }
        }
        #endregion

        [Test]
        public void AndroidTest()
        {
            Test(apps.OfType<AndroidApp>().SingleOrDefault());
        }

        [Test]
        public void iOSTest()
        {
            Test(apps.OfType<iOSApp>().SingleOrDefault());
        }

        #region Private Methods
        private void Test(IApp app)
        {
            if (app == null) throw new IgnoreException("No test app provided");
            
            app.Tap(c => c.Text("Run Tests"));

            app.WaitForElement(c => c.Text("Overall result:"), timeout: TimeSpan.FromSeconds(90));

            app.Tap(c => c.Text("Failed Results"));

            if (app.Query(c => c.Class("Xamarin_Forms_Platform_iOS_BoxRenderer")).Any())
            {
                app.Screenshot("Failures");

                throw new Exception($"{app.Query(c => c.Class("Xamarin_Forms_Platform_iOS_BoxRenderer")).Count()} Tests Failed");
            }
        }
        #endregion
    }
}
