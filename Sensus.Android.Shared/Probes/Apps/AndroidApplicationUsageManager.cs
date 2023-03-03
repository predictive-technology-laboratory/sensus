using Android.App;
using Android.App.Usage;
using Android.Content;
using Android.OS;
using Android.Provider;
using Sensus.Context;
using Sensus.Probes;
using System.Threading;
using System.Threading.Tasks;
using XamarinApplication = Xamarin.Forms.Application;

#warning AndroidApplicationUsageManager uses obsolete code.
#pragma warning disable CS0618 // Type or member is obsolete
namespace Sensus.Android.Probes.Apps
{
	public class AndroidApplicationUsageManager
	{
		private Probe _probe;
		//private static CancellationTokenSource _cancellationTokenSource;

		public UsageStatsManager Manager { get; }

		public AndroidApplicationUsageManager(Probe probe)
		{
			_probe = probe;
			Manager = (UsageStatsManager)Application.Context.GetSystemService(global::Android.Content.Context.UsageStatsService);
		}

		//private class AppOpsCallback : Java.Lang.Object, AppOpsManager.IOnOpChangedListener
		//{
		//	public void OnOpChanged(string op, string packageName)
		//	{
		//		if (op == AppOpsManager.OpstrGetUsageStats && _cancellationTokenSource != null)
		//		{
		//			_cancellationTokenSource.Cancel();
		//		}
		//	}
		//}

		public async Task CheckPermission()
		{
			AppOpsManager appOps = (AppOpsManager)Application.Context.GetSystemService(global::Android.Content.Context.AppOpsService);
			//AppOpsCallback callback = new AppOpsCallback();
			//appOps.StartWatchingMode(AppOpsManager.OpstrGetUsageStats, Application.Context.PackageName, new AppOpsCallback());

			if (appOps.CheckOpNoThrow(AppOpsManager.OpstrGetUsageStats, Process.MyUid(), Application.Context.PackageName) != AppOpsManagerMode.Allowed)
			//while (appOps.CheckOpNoThrow(AppOpsManager.OpstrGetUsageStats, Process.MyUid(), Application.Context.PackageName) != AppOpsManagerMode.Allowed)
			{
				bool continueStarting = await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
				{
					return await XamarinApplication.Current.MainPage.DisplayAlert("Permission Request", "Please enable the App Usage permission for Sensus", "OK", "Cancel");
				});


                //Check permission the second time to mitigate the case in which the permisions are asked twice. this happens if both ApplicationUdageEvent and ApplicationUsageStats probes are enabled
                if (appOps.CheckOpNoThrow(AppOpsManager.OpstrGetUsageStats, Process.MyUid(), Application.Context.PackageName) != AppOpsManagerMode.Allowed)
                {
                    if (continueStarting)
                    {
                        
                         Intent appUsageSettings = new Intent(Settings.ActionUsageAccessSettings);
                         appUsageSettings.AddFlags(ActivityFlags.NewTask);
                         Application.Context.StartActivity(appUsageSettings);
                        
                        //AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

                        //_cancellationTokenSource = new CancellationTokenSource();

                        //serviceHelper.WaitForFocus(_cancellationTokenSource.Token);

                        //_cancellationTokenSource.Dispose();
                        //_cancellationTokenSource = null;
                    }
                    else
                    {
                        _probe.Protocol.CancelStart();

                        return;
                    }
                }

                
			}

			//appOps.StopWatchingMode(callback);
		}
	}
}
#pragma warning restore CS0618 // Type or member is obsolete
