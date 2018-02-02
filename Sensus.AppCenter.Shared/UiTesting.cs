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
using System.Collections.Generic;
using System.Linq;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace Sensus.AppCenter.Shared
{
    public class UiTesting
    {
        public static void TestApp(IApp app, string labelClass)
        {
            // run tests and wait for results
            app.Tap(c => c.Text("Run Tests"));
            app.WaitForElement(c => c.Text("Overall result:"), timeout: TimeSpan.FromSeconds(90));

            // get and parse label content
            List<AppResult> labels = app.Query(q => q.Class(labelClass)).ToList();
            string overallResult = labels[labels.FindIndex(label => label.Text.Trim() == "Overall result:") + 1].Text;
            int testsRun = int.Parse(labels[labels.FindIndex(label => label.Text.Trim() == "Tests run:") + 1].Text);
            int testsPassed = int.Parse(labels[labels.FindIndex(label => label.Text.Trim() == "Passed:") + 1].Text);

            // check results
            if (overallResult != "Passed" || testsPassed != testsRun)
            {
                app.Tap(c => c.Text("Failed Results"));
                app.Screenshot("Failures");
                throw new Exception($"{testsPassed} of {testsRun} passed.");
            }
        }
    }
}
