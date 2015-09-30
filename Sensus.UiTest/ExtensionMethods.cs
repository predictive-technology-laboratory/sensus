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
using Xamarin.UITest;

namespace Sensus.UiTest
{
    public static class ExtensionMethods
    {
        public static void WaitForElementThenTap(this IApp app, string element, TimeSpan? timeout)
        {
            app.WaitForElement(element, timeout: timeout);
            app.Tap(element);
        }

        public static void WaitForElementThenEnterText(this IApp app, string element, string text, TimeSpan? timeout)
        {
            app.WaitForElement(element, timeout: timeout);
            app.EnterText(element, text);
        }

        public static void WaitFor(this IApp app, TimeSpan duration)
        {
            try
            {
                app.WaitForElement(Guid.NewGuid().ToString(), timeout: duration);
            }
            catch (TimeoutException)
            {
            }
        }
    }
}