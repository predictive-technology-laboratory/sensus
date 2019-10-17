using Android.App;
using Android.App.Usage;
using Android.Content.PM;
using Sensus.Probes.Apps;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Sensus.Android.Probes.Apps
{
	public class AndroidApplicationUsageEventProbe : ApplicationUsageEventProbe
	{
		AndroidApplicationUsageManager _manager = null;

		protected async override Task InitializeAsync()
		{
			await base.InitializeAsync();

			_manager = new AndroidApplicationUsageManager(this);

			await _manager.CheckPermission();
		}

		protected async override Task ProtectedStopAsync()
		{
			await base.ProtectedStopAsync();
		}

		protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
		{
			long now = Java.Lang.JavaSystem.CurrentTimeMillis();
			long startTime = now - PollingSleepDurationMS;
			UsageEvents events = _manager.Manager.QueryEvents(startTime, now);
			List<Datum> data = new List<Datum>();

			while (events.HasNextEvent)
			{
				UsageEvents.Event usageEvent = new UsageEvents.Event();

				events.GetNextEvent(usageEvent);

				string applicationName = Application.Context.PackageManager.GetApplicationLabel(Application.Context.PackageManager.GetApplicationInfo(usageEvent.PackageName, PackageInfoFlags.MatchDefaultOnly));

				data.Add(new ApplicationUsageEventDatum(usageEvent.PackageName, applicationName, usageEvent.EventType.ToString(), DateTimeOffset.FromUnixTimeMilliseconds(usageEvent.TimeStamp)));
			}

			return Task.FromResult(data);
		}
	}
}
