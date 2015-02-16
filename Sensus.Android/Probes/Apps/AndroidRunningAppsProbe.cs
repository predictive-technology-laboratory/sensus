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

using Android.App;
using SensusService.Probes.Apps;
using System;
using System.Collections.Generic;

namespace Sensus.Android.Probes.Apps
{
    public class AndroidRunningAppsProbe : RunningAppsProbe
    {
        private ActivityManager _activityManager;

        public AndroidRunningAppsProbe()
        {
            _activityManager = Application.Context.GetSystemService(global::Android.Content.Context.ActivityService) as ActivityManager;
        }

        protected override List<RunningAppsDatum> GetRunningAppsData()
        {
            List<RunningAppsDatum> data = new List<RunningAppsDatum>();

            foreach (ActivityManager.RunningTaskInfo task in _activityManager.GetRunningTasks(int.MaxValue))
            {
                string name = task.BaseActivity.PackageName;
                string desc = task.Description == null ? "" : task.Description.ToString();
                data.Add(new RunningAppsDatum(this, DateTimeOffset.UtcNow, name, desc));
            }

            return data;
        }
    }
}
