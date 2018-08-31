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
using Foundation;
using Sensus.Context;
using Sensus.iOS.Concurrent;
using Sensus.Tests.Classes;
using UIKit;
using Xunit;
using Xunit.Runner;
using Xunit.Sdk;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Sensus.iOS.Tests
{
    [Register("AppDelegate")]
    public class AppDelegate : RunnerAppDelegate
    {
        public override bool FinishedLaunching(UIApplication uiApplication, NSDictionary launchOptions)
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
                InvokeOnMainThread(() =>
                {
                    UIAlertView alertView = new UIAlertView("Results", results, default(IUIAlertViewDelegate), "Close");
                    alertView.AccessibilityLabel = "unit-test-results";
                    alertView.Show();
                });
            };

            ResultChannel = resultCounter;

#if ENABLE_TEST_CLOUD
            Xamarin.Calabash.Start();
#endif

            return base.FinishedLaunching(uiApplication, launchOptions);
		}
    }
}