using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Sensus.Probes;
using Sensus.UI;
using Syncfusion.SfChart.XForms;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using XamarinApplication = Xamarin.Forms.Application;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidApplicationUsageEventProbe : PollingProbe
	{
		protected UsageStatsManager _manager;

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

		protected override Task InitializeAsync()
		{
			_manager = (UsageStatsManager)Application.Context.GetSystemService(global::Android.Content.Context.UsageStatsService);

			return base.InitializeAsync();
		}

		protected override async Task ProtectedStartAsync()
		{
			AppOpsManager appOps = (AppOpsManager)Application.Context.GetSystemService(global::Android.Content.Context.AppOpsService);

			if (appOps.CheckOpNoThrow(AppOpsManager.OpstrGetUsageStats, Process.MyUid(), Application.Context.PackageName) != AppOpsManagerMode.Allowed)
			{
				await XamarinApplication.Current.MainPage.DisplayAlert("Sensus", "Sensus requires access to app usage data. It can be granted on the following screen.", "Close");

				Application.Context.StartActivity(new Intent(global::Android.Provider.Settings.ActionUsageAccessSettings));
			}

			await base.ProtectedStartAsync();
		}

		protected override ChartDataPoint GetChartDataPointFromDatum(Datum datum)
		{
			throw new NotImplementedException();
		}

		protected override ChartAxis GetChartPrimaryAxis()
		{
			throw new NotImplementedException();
		}

		protected override RangeAxisBase GetChartSecondaryAxis()
		{
			throw new NotImplementedException();
		}

		protected override ChartSeries GetChartSeries()
		{
			throw new NotImplementedException();
		}
	}
}
