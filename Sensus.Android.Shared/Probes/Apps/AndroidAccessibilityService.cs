using Android;
using Android.AccessibilityServices;
using Android.App;
using Android.Content;
using Android.Views.Accessibility;
using Android.Provider;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XamarinApp = Xamarin.Forms.Application;
using Sensus.Context;
using System.Threading;

namespace Sensus.Android.Probes.Apps
{
	//[Service(Permission = Manifest.Permission.BindAccessibilityService, Exported = false)]
	//[IntentFilter(new[] { "android.accessibilityservice.AccessibilityService" })]
	//[MetaData("android.accessibilityservice", Resource = "@xml/accessibilityservice")] // this is currently disabled due to the fact that enabling "touch exploration mode" renders the phone unusable
	public class AndroidAccessibilityService : AccessibilityService
	{
		private static readonly List<AndroidAccessibilityProbe> _probes;
		//private static CancellationTokenSource _cancellationTokenSource;

		static AndroidAccessibilityService()
		{
			_probes = new List<AndroidAccessibilityProbe>();
		}

		private static bool IsServiceEnabled()
		{
			AccessibilityManager manager = (AccessibilityManager)Application.Context.GetSystemService(AccessibilityService);

			return manager.GetEnabledAccessibilityServiceList(FeedbackFlags.AllMask).Any(x => x.ResolveInfo.ServiceInfo.PackageName == Application.Context.ApplicationInfo.PackageName);
		}

		public static async Task RegisterProbeAsync(AndroidAccessibilityProbe probe)
		{
			lock (_probes)
			{
				if (probe != null && _probes.Contains(probe) == false)
				{
					_probes.Add(probe);
				}
			}

			if (IsServiceEnabled() == false)
			//while (IsServiceEnabled() == false)
			{
				bool continueStarting = await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
				{
					return await XamarinApp.Current.MainPage.DisplayAlert("Permission Request", "Please enable the Accessibility Service permission for Sensus", "OK", "Cancel");
				});

				if (continueStarting)
				{
					Intent accessibilitySettings = new Intent(Settings.ActionAccessibilitySettings);
					accessibilitySettings.AddFlags(ActivityFlags.NewTask);
					Application.Context.StartActivity(accessibilitySettings);

					//AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

					//_cancellationTokenSource = new CancellationTokenSource();

					//serviceHelper.WaitForFocus(_cancellationTokenSource.Token);

					//_cancellationTokenSource.Dispose();
					//_cancellationTokenSource = null;
				}
				else
				{
					probe.Protocol.CancelStart();

					return;
				}
			}
		}

		public static Task UnregisterProbeAsync(AndroidAccessibilityProbe probe)
		{
			lock (_probes)
			{
				_probes.Remove(probe);
			}

			return Task.CompletedTask;
		}

		public override void OnAccessibilityEvent(AccessibilityEvent e)
		{
			lock (_probes)
			{
				Task.WaitAll(_probes.Select(p => p.OnAccessibilityEventAsync(e)).ToArray());
			}
		}

		public override void OnInterrupt()
		{

		}

		protected override void OnServiceConnected()
		{
			base.OnServiceConnected();

			AccessibilityServiceInfo info = new AccessibilityServiceInfo()
			{
				EventTypes = EventTypes.AllMask,
				FeedbackType = FeedbackFlags.AllMask,
				NotificationTimeout = 500,
				Flags = AccessibilityServiceFlags.Default
			};

			SetServiceInfo(info);

			//if (_cancellationTokenSource != null)
			//{
			//	_cancellationTokenSource.Cancel();
			//}
		}
	}
}
