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
using Android.App.Usage;
using Android.Content.PM;
using Sensus.Probes.Apps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidApplicationUsageStatsProbe : ApplicationUsageStatsProbe
	{
		protected async override Task InitializeAsync()
		{
			await base.InitializeAsync();

			if (await SensusServiceHelper.Get().ObtainPermissionAsync<AndroidPermissions.UsageStats>() == PermissionStatus.Granted)
			{
				if (AndroidSensusServiceHelper.UsageStatsManager == null)
				{
					throw new NotSupportedException("No usage stats present.");
				}
			}
			else
			{
				// throw standard exception instead of NotSupportedException, since the user might decide to enable location in the future
				// and we'd like the probe to be restarted at that time.
				string error = "Querying usage stats is not permitted on this device. Cannot start usage stats probe.";
				await SensusServiceHelper.Get().FlashNotificationAsync(error);
				throw new Exception(error);
			}
		}

		protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
		{
			long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			long startTime = now - PollingSleepDurationMS;
			List<UsageStats> usageStats = AndroidSensusServiceHelper.UsageStatsManager.QueryAndAggregateUsageStats(startTime, now).Select(x => x.Value).ToList();
			List<Datum> data = new List<Datum>();

			foreach (UsageStats usage in usageStats)
			{
				string applicationName = Application.Context.PackageManager.GetApplicationLabel(Application.Context.PackageManager.GetApplicationInfo(usage.PackageName, PackageInfoFlags.MatchDefaultOnly));

				data.Add(new ApplicationUsageStatsDatum(usage.PackageName, applicationName, DateTimeOffset.FromUnixTimeMilliseconds(usage.FirstTimeStamp), DateTimeOffset.FromUnixTimeMilliseconds(usage.LastTimeStamp), DateTimeOffset.FromUnixTimeMilliseconds(usage.LastTimeUsed), TimeSpan.FromMilliseconds(usage.TotalTimeInForeground), DateTimeOffset.UtcNow));
			}

			return Task.FromResult(data);
		}
	}
}
