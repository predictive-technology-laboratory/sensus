using Android.App;
using Android.App.Usage;
using Android.Content.PM;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Sensus.Android.Probes.Apps
{
	public class AndroidApplicationUsageEventProbe : AndroidApplicationUsageProbe
	{
		public override int DefaultPollingSleepDurationMS => (int)TimeSpan.FromHours(1).TotalMilliseconds;

		public override string DisplayName => "Application Events";

		public override Type DatumType => typeof(ApplicationUsageEventDatum);

		protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
		{
			long now = Java.Lang.JavaSystem.CurrentTimeMillis();
			long startTime = now - PollingSleepDurationMS;
			UsageEvents events = _manager.QueryEvents(startTime, now);
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
