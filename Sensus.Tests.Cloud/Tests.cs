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
using Xamarin.UITest.Queries;

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
#if __ANDROID__
                apps.Add(ConfigureApp.Android.StartApp());
#elif __IOS__
                apps.Add(ConfigureApp.iOS.StartApp());
#endif
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
            if (app == null)
            {
                throw new IgnoreException("No test app provided");
            }

            app.Tap(c => c.Text("Run Tests"));
            app.WaitForElement(c => c.Text("Overall result:"), timeout: TimeSpan.FromSeconds(90));

            AppResult[] textBoxes = app.Query(q => q.Class("FormsTextView")).ToArray();

            Assert.AreEqual(textBoxes[0].Text.Trim(), "Overall result:");
            string overallResult = textBoxes[1].Text;

            Assert.AreEqual(textBoxes[2].Text.Trim(), "Tests run:");
            int testsRun = int.Parse(textBoxes[3].Text);

            Assert.AreEqual(textBoxes[4].Text.Trim(), "Passed:");
            int testsPassed = int.Parse(textBoxes[5].Text);

            if (overallResult != "Passed" || testsPassed != testsRun)
            {
                app.Tap(c => c.Text("Failed Results"));
                app.Screenshot("Failures");
                throw new Exception($"{testsPassed} of {testsRun} passed.");
            }
        }
        #endregion
    }
}
