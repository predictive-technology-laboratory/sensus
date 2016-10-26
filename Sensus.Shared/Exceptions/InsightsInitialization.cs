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

using Xamarin;

namespace Sensus.Exceptions
{
    public static class InsightsInitialization
    {
        public static void Initialize(IInsightsInitializer initializer, string insightsKey, bool suppressException = true)
        {
            if (string.IsNullOrWhiteSpace(insightsKey))
                return;

            try
            {
                // see https://developer.xamarin.com/guides/insights/platform-features/advanced-topics/dealing-with-startup-crashes/
                Insights.HasPendingCrashReport += (sender, isStartupCrash) =>
                {
                    if (isStartupCrash)
                    {
                        Insights.PurgePendingCrashReports().Wait();
                    }
                };

                initializer.Initialize(insightsKey);
            }
            catch
            {
                // If we fail to setup our error report code no reason to throw an exception we'll never see. Godspeed user.
                if (!suppressException)
                    throw;
            }
        }
    }
}