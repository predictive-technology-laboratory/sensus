using Android.App;
using Android.App.Usage;
using Android.Content.PM;
using Sensus.Probes.Apps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidApplicationUsageStatsProbe : ApplicationUsageStatsProbe
	{
		AndroidApplicationUsageManager _manager = null;

		protected async override Task InitializeAsync()
		{
			await base.InitializeAsync();

			_manager = new AndroidApplicationUsageManager();

			await _manager.CheckPermission();
		}

		protected async override Task ProtectedStopAsync()
		{
			await base.ProtectedStopAsync();

			_manager.DecrementPermission();
		}

		protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
		{
			long now = Java.Lang.JavaSystem.CurrentTimeMillis();
			long startTime = now - PollingSleepDurationMS;
			List<UsageStats> usageStats = _manager.Manager.QueryAndAggregateUsageStats(startTime, now).Select(x => x.Value).ToList();
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
