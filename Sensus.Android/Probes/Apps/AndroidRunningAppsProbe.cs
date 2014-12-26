using Android.App;
using Android.Content;
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