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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidApplicationUsageEventProbe : ApplicationUsageEventProbe
	{
		protected async override Task InitializeAsync()
		{
			await base.InitializeAsync();

			if (await SensusServiceHelper.Get().ObtainPermissionAsync<AndroidPermissions.UsageStats>() == PermissionStatus.Granted)
			{
				if (AndroidSensusServiceHelper.UsageStatsManager == null)
				{
					throw new NotSupportedException("No usage events present.");
				}
			}
			else
			{
				// throw standard exception instead of NotSupportedException, since the user might decide to enable location in the future
				// and we'd like the probe to be restarted at that time.
				string error = "Querying usage events is not permitted on this device. Cannot start usage event probe.";
				await SensusServiceHelper.Get().FlashNotificationAsync(error);
				throw new Exception(error);
			}
		}

		protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
		{
			long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			long startTime = now - PollingSleepDurationMS;
			UsageEvents events = AndroidSensusServiceHelper.UsageStatsManager.QueryEvents(startTime, now);
			List<Datum> data = new List<Datum>();

			while (events.HasNextEvent)
			{
				UsageEvents.Event usage = new UsageEvents.Event();

				events.GetNextEvent(usage);

				string applicationName = Application.Context.PackageManager.GetApplicationLabel(Application.Context.PackageManager.GetApplicationInfo(usage.PackageName, PackageInfoFlags.MatchDefaultOnly));

				data.Add(new ApplicationUsageEventDatum(usage.PackageName, applicationName, usage.EventType.ToString(), DateTimeOffset.FromUnixTimeMilliseconds(usage.TimeStamp)));
			}

			return Task.FromResult(data);
		}
	}
}
