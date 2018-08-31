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
using System;
using Xunit.Runners;
using System.Threading.Tasks;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Sensus.Android.Tests
{
    [Activity(Label = "xUnit Android Runner", MainLauncher = true, Theme= "@android:style/Theme.Material.Light")]
    public class MainActivity : RunnerActivity, IResultChannel
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

            ResultChannel = this;

            base.OnCreate(bundle);
        }

        public Task CloseChannel()
        {
            return Task.CompletedTask;
        }

        public Task<bool> OpenChannel(string message = null)
        {
            return Task.FromResult(true);
        }

        public void RecordResult(TestResultViewModel result)
        {
            Console.Out.WriteLine("Error:  " + result.ErrorMessage);
        }
    }
}