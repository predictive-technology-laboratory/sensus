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

using System.Reflection;
using Android.App;
using Android.OS;
using Xunit.Sdk;
using Xunit.Runners.UI;
using Sensus.Context;
using Sensus.Tests.Classes;
using Sensus.Android.Concurrent;
using Xunit;
using Android.Widget;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Sensus.Android.Tests
{
    [Activity(Label = "xUnit Android Runner", MainLauncher = true, Theme= "@android:style/Theme.Material.Light")]
    public class MainActivity : RunnerActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            SensusContext.Current = new TestSensusContext
            {
                MainThreadSynchronizer = new MainConcurrent()
            };

            AddTestAssembly(Assembly.GetExecutingAssembly());
            AddExecutionAssembly(typeof(ExtensibilityPointFactory).Assembly);
			AutoStart = true;
            TerminateAfterExecution = false;

            XUnitResultCounter resultCounter = new XUnitResultCounter();

            resultCounter.ResultsDelivered += (sender, results) => 
            {
                RunOnUiThread(() =>
                {
                    TextView resultView = new TextView(this)
                    {
                        ContentDescription = "unit-test-results",
                        Text = results
                    };

                    new AlertDialog.Builder(this).SetView(resultView).Show();
                });
            };

            ResultChannel = resultCounter;

            base.OnCreate(bundle);
        }
    }
}