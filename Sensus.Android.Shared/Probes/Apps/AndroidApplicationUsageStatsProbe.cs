using Android.App;
using Android.App.Usage;
using Android.Content.PM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XamarinApplication = Xamarin.Forms.Application;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidApplicationUsageStatsProbe : AndroidApplicationUsageProbe
	{
		public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromHours(1).TotalMilliseconds;

		public override string DisplayName => "Application Stats";

		public override Type DatumType => typeof(ApplicationUsageEventDatum);

		protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
		{
			long now = Java.Lang.JavaSystem.CurrentTimeMillis();
			long startTime = now - PollingSleepDurationMS;
			List<UsageStats> usageStats = _manager.QueryAndAggregateUsageStats(startTime, now).Select(x => x.Value).ToList();
			List<Datum> data = new List<Datum>();

			foreach(UsageStats appStats in usageStats)
			{
				string applicationName = Application.Context.PackageManager.GetApplicationLabel(Application.Context.PackageManager.GetApplicationInfo(appStats.PackageName, PackageInfoFlags.MatchDefaultOnly));

				data.Add(new ApplicationUsageStatsDatum(appStats.PackageName, applicationName, DateTimeOffset.FromUnixTimeMilliseconds(appStats.FirstTimeStamp), DateTimeOffset.FromUnixTimeMilliseconds(appStats.LastTimeStamp), DateTimeOffset.FromUnixTimeMilliseconds(appStats.LastTimeUsed), TimeSpan.FromMilliseconds(appStats.TotalTimeInForeground), DateTimeOffset.UtcNow));
			}

			return Task.FromResult(data);
		}
	}
}
