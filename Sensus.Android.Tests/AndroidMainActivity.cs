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
using Android.App;
using Android.OS;
using Xunit.Sdk;
using Xunit.Runners.UI;
using Sensus.Context;
using Sensus.Tests.Classes;
using Sensus.Android.Concurrent;
using Xunit;
using Android.Widget;
using Android.Content;
using System;
using Xamarin.Facebook;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

// need to use the same namespace as used within the main android app project, as we're pulling in code
// from the shared project that references the class below.
namespace Sensus.Android
{
    [Activity(Label = "xUnit Android Runner", MainLauncher = true, Theme = "@android:style/Theme.Material.Light")]
    public class AndroidMainActivity : RunnerActivity
    {
        public ICallbackManager FacebookCallbackManager { get; set; }

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

        public void GetActivityResultAsync(Intent intent, AndroidActivityResultRequestCode requestCode, Action<Tuple<Result, Intent>> callback)
        {
            throw new NotImplementedException();
        }
    }
}
