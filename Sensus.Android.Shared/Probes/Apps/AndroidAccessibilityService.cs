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
using Android.OS;

namespace Sensus.Android.Probes.Apps
{
	[Service(Permission = Manifest.Permission.BindAccessibilityService)]
	[IntentFilter(new[] { "android.accessibilityservice.AccessibilityService" })]
	//[MetaData("android.accessibilityservice", Resource = "@xml/serviceconfig")] // this is currently disabled due to the fact that enabling "touch exploration mode" renders the phone unusable
	public class AndroidAccessibilityService : AccessibilityService
	{
		private static readonly List<AndroidAccessibilityProbe> _probes;
		//private static readonly ManualResetEventSlim _event;
		//private static SettingActivtyState _settingsActivityState;
		
		//private enum SettingActivtyState
		//{
		//	DialogNotShown,
		//	DialogShown,
		//	DialogClosed
		//}

		static AndroidAccessibilityService()
		{
			_probes = new List<AndroidAccessibilityProbe>();
			//_event = new ManualResetEventSlim();
		}

		//public static void ContinueAfterAccessibilitySettings()
		//{
		//	if (_settingsActivityState == SettingActivtyState.DialogClosed)
		//	{
		//		_event.Set();

		//		_settingsActivityState = SettingActivtyState.DialogNotShown;
		//	}
		//	else if(_settingsActivityState == SettingActivtyState.DialogShown)
		//	{
		//		_settingsActivityState = SettingActivtyState.DialogClosed;
		//	}
		//}

		private static bool IsServiceEnabled()
		{
			AccessibilityManager manager = (AccessibilityManager)Application.Context.GetSystemService(AccessibilityService);

			return manager.GetEnabledAccessibilityServiceList(FeedbackFlags.AllMask).Any(x => x.ResolveInfo.ServiceInfo.PackageName == Application.Context.ApplicationInfo.PackageName);
		}

		public static async Task RegisterProbeAsync(AndroidAccessibilityProbe probe)
		{
			bool isFirstProbe = false;

			lock(_probes)
			{
				isFirstProbe = _probes.Any() == false;

				if (probe != null && _probes.Contains(probe) == false)
				{
					_probes.Add(probe);
				}
			}

			if (IsServiceEnabled() == false && isFirstProbe)
			{
				await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
				{
					//_settingsActivityState = SettingActivtyState.DialogShown;
					await XamarinApp.Current.MainPage.DisplayAlert("Permission Request", "On the next screen, please enable the Accessibility Service permission for Sensus", "OK");
				});

				Intent accessibilitySettings = new Intent(Settings.ActionAccessibilitySettings);
				accessibilitySettings.AddFlags(ActivityFlags.NewTask);
				Application.Context.StartActivity(accessibilitySettings);

				//_event.Wait();
				//_settingsActivityState = SettingActivtyState.DialogNotShown;
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
			//string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

			//using (System.IO.StreamWriter sw = new System.IO.StreamWriter(System.IO.Path.Combine(path, $"{System.Guid.NewGuid()}.json")))
			//{
			//	sw.Write(Newtonsoft.Json.JsonConvert.SerializeObject(e));
			//}

			//var documents = 
			//var filename = System.IO.Path.Combine(documents, $"{System.Guid.NewGuid()}.json");
			//System.IO.File.WriteAllText(filename, "Write this text into a file");

			//string json = Newtonsoft.Json.JsonConvert.SerializeObject(e.GetType().GetProperties().ToDictionary(x => x.Name, x => x.GetValue(e)?.ToString()));

			//SensusServiceHelper.Get().Logger.Log(json, LoggingLevel.Normal, GetType());

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

			//if (SensusServiceHelper.Get() is AndroidSensusServiceHelper serviceHelper)
			//{
			//	serviceHelper.AccessibilityService = this;
			//}

			//_event.Set();
		}
	}
}
