using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.OS;
using Sensus.Context;
using Sensus.Probes;
using Syncfusion.SfChart.XForms;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using XamarinApplication = Xamarin.Forms.Application;

namespace Sensus.Android.Probes.Apps
{
	public abstract class AndroidApplicationUsageProbe : PollingProbe
	{
		protected UsageStatsManager _manager;

		public override int DefaultPollingSleepDurationMS => throw new NotImplementedException();

		public override string DisplayName => throw new NotImplementedException();

		public override Type DatumType => throw new NotImplementedException();

		protected override Task InitializeAsync()
		{
			_manager = (UsageStatsManager)Application.Context.GetSystemService(global::Android.Content.Context.UsageStatsService);

			return base.InitializeAsync();
		}

		protected override Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		protected override async Task ProtectedStartAsync()
		{
			AppOpsManager appOps = (AppOpsManager)Application.Context.GetSystemService(global::Android.Content.Context.AppOpsService);

			if (appOps.CheckOpNoThrow(AppOpsManager.OpstrGetUsageStats, Process.MyUid(), Application.Context.PackageName) != AppOpsManagerMode.Allowed)
			{
				await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
				{
					await XamarinApplication.Current.MainPage.DisplayAlert("Sensus", "Sensus requires access to app usage data. It can be granted on the following screen.", "Close");
				});

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
