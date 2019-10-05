using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.OS;
using Sensus.Context;
using System;
using System.Threading.Tasks;
using XamarinApplication = Xamarin.Forms.Application;

namespace Sensus.Android.Probes.Apps
{
	public class AndroidApplicationUsageManager
	{
		public UsageStatsManager Manager { get; }
		private static int _permissionChecked = 0;

		public AndroidApplicationUsageManager()
		{
			Manager = (UsageStatsManager)Application.Context.GetSystemService(global::Android.Content.Context.UsageStatsService);
		}

		public async Task CheckPermission()
		{
			if (_permissionChecked == 0)
			{
				AppOpsManager appOps = (AppOpsManager)Application.Context.GetSystemService(global::Android.Content.Context.AppOpsService);

				if (appOps.CheckOpNoThrow(AppOpsManager.OpstrGetUsageStats, Process.MyUid(), Application.Context.PackageName) != AppOpsManagerMode.Allowed)
				{
					await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
					{
						await XamarinApplication.Current.MainPage.DisplayAlert("Sensus", "Sensus requires access to app usage data. It can be granted on the following screen.", "Close");
					});

					Application.Context.StartActivity(new Intent(global::Android.Provider.Settings.ActionUsageAccessSettings));

					_permissionChecked += 1;
				}
			}
		}

		public void DecrementPermission()
		{
			_permissionChecked = Math.Max(0, _permissionChecked - 1);
		}
	}
}
