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
                    new UIAlertView("Results", results, default(IUIAlertViewDelegate), "Close").Show();
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
