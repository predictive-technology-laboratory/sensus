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

        public override int DefaultPollingSleepDurationMS
        {
            get { return 1000 * 60; }
        }

        public AndroidRunningAppsProbe()
        {
            _activityManager = Application.Context.GetSystemService(Context.ActivityService) as ActivityManager;
        }

        protected override List<RunningAppsDatum> GetRunningAppsData()
        {
            List<RunningAppsDatum> data = new List<RunningAppsDatum>();

            foreach (ActivityManager.RunningTaskInfo task in _activityManager.GetRunningTasks(MaxAppsPerPoll))
                data.Add(new RunningAppsDatum(this, DateTimeOffset.UtcNow, task.Class.CanonicalName, task.Description.ToString()));

            return data;
        }
    }
}