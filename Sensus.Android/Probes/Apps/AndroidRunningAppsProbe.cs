using Android.App;
using Android.Content;
using SensusService;
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
            _activityManager = Application.Context.GetSystemService(Context.ActivityService) as ActivityManager;
        }

        public override IEnumerable<Datum> Poll()
        {
            List<RunningAppsDatum> data = new List<RunningAppsDatum>();

            foreach (ActivityManager.RunningTaskInfo task in _activityManager.GetRunningTasks(MaximumNumber))
                data.Add(new RunningAppsDatum(this, DateTimeOffset.UtcNow, task.Class.CanonicalName, task.Description.ToString()));

            return data;
        }
    }
}