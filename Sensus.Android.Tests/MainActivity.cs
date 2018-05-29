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
using System.IO;
using System.Reflection;
using System.Text;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using Sensus.Android.Tests.SetUp;
using Sensus.Tests.Classes;
using Xamarin.Android.NUnitLite;

namespace Sensus.Android.Tests
{
    [Activity(Label = "Sensus.Android.Tests", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : TestSuiteActivity
    {
        private LogSaver _logSaver = new LogSaver();

        protected override void OnCreate(Bundle bundle)
        {
            Console.SetOut(_logSaver);
            SetUpFixture.SetUp();

            AddTest(Assembly.GetExecutingAssembly());

            // to add tests in additional assemblies, uncomment/adapt the line below
            // AddTest (typeof (Your.Library.TestClass).Assembly);

            // there is currently an issue where the test runner blocks the UI thread.
            Intent.PutExtra("automated", true);

            // Once you called base.OnCreate(), you cannot add more assemblies.
            base.OnCreate(bundle);
        }

        public override void Finish()
        {
            RunOnUiThread(() =>
            {
                TextView logView = new TextView(this)
                {
                    ContentDescription = "sensus-test-log",
                    Text = _logSaver.Log.ToString().Trim()
                };

                ScrollView logScroll = new ScrollView(this);
                logScroll.AddView(logView);

                SetContentView(logScroll);
            });
        }
    }
}
