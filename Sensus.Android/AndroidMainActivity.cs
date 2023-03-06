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

using System;
using System.IO;
using System.Threading;
using Android.OS;
using Android.App;
using Android.Widget;
using Android.Content;
using Android.Content.PM;
using Sensus.UI;
using Sensus.Context;
using Xamarin;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Plugin.CurrentActivity;
using System.Threading.Tasks;
using Sensus.Exceptions;
using Sensus.Notifications;
using Xamarin.Essentials;
using Platform = Xamarin.Essentials.Platform;
//using Sensus.Android.Probes.Apps.Accessibility;

namespace Sensus.Android
{
	[Activity(Label = "SensusMobile", MainLauncher = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	[IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "https", DataHost = "*", DataPathPattern = ".*\\\\.json")]  // protocols downloaded from an https web link
	[IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault }, DataMimeType = "application/json")]  // protocols obtained from "file" and "content" schemes:  http://developer.android.com/guide/components/intents-filters.html#DataTest
	public class AndroidMainActivity : FormsAppCompatActivity
	{
		private AndroidSensusServiceConnection _serviceConnection;
		private ManualResetEvent _activityResultWait;
		private AndroidActivityResultRequestCode _activityResultRequestCode;
		private Tuple<Result, Intent> _activityResult;
		private ManualResetEvent _serviceBindWait;

		private readonly object _locker = new object();

		protected override async void OnCreate(Bundle savedInstanceState)
		{
			Console.Error.WriteLine("--------------------------- Creating activity ---------------------------");

			// set the layout resources first
			ToolbarResource = Resource.Layout.Toolbar;
			TabLayoutResource = Resource.Layout.Tabbar;

			base.OnCreate(savedInstanceState);

			_activityResultWait = new ManualResetEvent(false);
			_serviceBindWait = new ManualResetEvent(false);



			Window.AddFlags(global::Android.Views.WindowManagerFlags.DismissKeyguard);
			Window.AddFlags(global::Android.Views.WindowManagerFlags.ShowWhenLocked);
			Window.AddFlags(global::Android.Views.WindowManagerFlags.TurnScreenOn);

			Forms.Init(this, savedInstanceState);
			FormsMaps.Init(this, savedInstanceState);
			Platform.Init(this, savedInstanceState);

			ZXing.Net.Mobile.Forms.Android.Platform.Init();

#if UI_TESTING
            Forms.ViewInitialized += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.View.StyleId))
                {
                    e.NativeView.ContentDescription = e.View.StyleId;
                }
            };
#endif

			LoadApplication(new App());

			_serviceConnection = new AndroidSensusServiceConnection();
			_serviceConnection.ServiceConnected += (o, e) =>
			{
				// service was created/connected, so the service helper must exist.
				SensusServiceHelper.Get().Logger.Log("Bound to Android service.", LoggingLevel.Normal, GetType());

				// tell the service to finish this activity when it is stopped
				e.Binder.OnServiceStop = Finish;

				// signal the activity that the service has been bound
				_serviceBindWait.Set();

				// if we're UI testing, try to load and run the UI testing protocol from the embedded assets
#if UI_TESTING
                using (Stream protocolFile = Assets.Open("UiTestingProtocol.json"))
                {
                    Protocol.RunUiTestingProtocolAsync(protocolFile);
                }
#endif
			};

			// the following is fired if the process hosting the service crashes or is killed.
			_serviceConnection.ServiceDisconnected += (o, e) =>
			{
				Toast.MakeText(this, "The Sensus service has crashed.", ToastLength.Long);
				DisconnectFromService();
				Finish();
			};

			// ensure the service is started any time the activity is created
			AndroidSensusService.Start(false);

			await OpenIntentAsync(Intent);
		}

		protected override void OnStart()
		{
			Console.Error.WriteLine("--------------------------- Starting activity ---------------------------");

			base.OnStart();
		}

		protected override async void OnResume()
		{
			Console.Error.WriteLine("--------------------------- Resuming activity ---------------------------");

			base.OnResume();

			// temporarily hide UI while we bind to service. allowing the user to click around before the service helper is initialized 
			// may result in a crash.
			(Xamarin.Forms.Application.Current as App).FlyoutPage.IsVisible = false;
			(Xamarin.Forms.Application.Current as App).DetailPage.IsVisible = false;

			// ensure the service is bound any time the activity is resumed
			BindService(AndroidSensusService.GetServiceIntent(false), _serviceConnection, Bind.AboveClient);

			// start new task to wait for connection, since we're currently on the UI thread, which the service connection needs in order to complete.
			await Task.Run(() =>
			{
				// we've not seen the binding take more than a second or two; however, we want to be very careful not to block indefinitely
				// here because the UI is currently disabled. if for some strange reason the binding does not work, bail out after 10 seconds
				// and let the user interact with the UI. most likely, a crash will be coming very soon in this case, as the sensus service
				// will probably not be running. again, this has not occurred in practice, but allowing the crash to occur will send us information
				// through the crash analytics service and we'll be able to track it
				TimeSpan serviceBindTimeout = TimeSpan.FromSeconds(10000);
				if (_serviceBindWait.WaitOne(serviceBindTimeout))
				{
					SensusServiceHelper.Get().Logger.Log("Activity proceeding following service bind.", LoggingLevel.Normal, GetType());
				}
				else
				{
					SensusException.Report("Timed out waiting " + serviceBindTimeout + " for the service to bind.");
				}

				SensusServiceHelper.Get().CancelPendingSurveysNotification();

				// enable the UI
				try
				{
					SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
					{
						(Xamarin.Forms.Application.Current as App).FlyoutPage.IsVisible = true;
						(Xamarin.Forms.Application.Current as App).DetailPage.IsVisible = true;
					});
				}
				catch (Exception)
				{
				}
			});
		}

		protected override void OnPause()
		{
			Console.Error.WriteLine("--------------------------- Pausing activity ---------------------------");

			base.OnPause();

			// we disconnect from the service within onpause because onresume always blocks the user while rebinding
			// to the service. conditions (the bind wait handle and service connection) need to be ready for onresume
			// and this is the only place to establish those conditions.
			DisconnectFromService();
		}

		protected override async void OnStop()
		{
			Console.Error.WriteLine("--------------------------- Stopping activity ---------------------------");

			base.OnStop();

			SensusServiceHelper serviceHelper = SensusServiceHelper.Get();

			if (serviceHelper != null)
			{
				await serviceHelper.SaveAsync();
			}
		}

		protected override void OnDestroy()
		{
			Console.Error.WriteLine("--------------------------- Destroying activity ---------------------------");

			base.OnDestroy();

			// if the activity is destroyed, reset the service connection stop action to be null so that the service doesn't try to
			// finish a destroyed activity if/when the service stops.
			if (_serviceConnection.Binder != null)
			{
				_serviceConnection.Binder.OnServiceStop = null;
			}
		}

		private void DisconnectFromService()
		{
			_serviceBindWait.Reset();

			if (_serviceConnection.Binder != null)
			{
				try
				{
					UnbindService(_serviceConnection);
					SensusServiceHelper.Get().Logger.Log("Unbound from Android service.", LoggingLevel.Normal, GetType());
				}
				catch (Exception ex)
				{
					SensusServiceHelper.Get().Logger.Log("Failed to disconnect from service:  " + ex.Message, LoggingLevel.Normal, GetType());
				}
			}
		}

		public override void OnWindowFocusChanged(bool hasFocus)
		{
			base.OnWindowFocusChanged(hasFocus);

			// the service helper is responsible for running actions that depend on the main activity. if the main activity
			// is not showing, the service helper starts the main activity and then runs requested actions. there is a race
			// condition between actions that wish to show a dialog (e.g., starting speech recognition) and the display of
			// the activity. in order to ensure that the activity is showing before any actions are run, we override this
			// focus changed event and let the service helper know when the activity is focused and when it is not. this
			// way, any actions that the service helper runs will certainly be run after the main activity is running
			// and focused.
			AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

			if (serviceHelper != null)
			{
				if (hasFocus)
				{
					serviceHelper.SetFocusedMainActivity(this);
				}
				else
				{
					serviceHelper.SetFocusedMainActivity(null);
				}
			}
		}

		#region intent handling

		protected override async void OnNewIntent(Intent intent)
		{
			base.OnNewIntent(intent);

			await OpenIntentAsync(intent);
		}

		private async Task OpenIntentAsync(Intent intent)
		{
			// wait for service helper to be initialized, since this method might be called before the service starts up
			// and initializes the service helper.
			int timeToWaitMS = 60000;
			int waitIntervalMS = 1000;
			while (SensusServiceHelper.Get() == null && timeToWaitMS > 0)
			{
				await Task.Delay(waitIntervalMS);
				timeToWaitMS -= waitIntervalMS;
			}

			if (SensusServiceHelper.Get() == null)
			{
				// don't use SensusServiceHelper.Get().FlashNotificationAsync because service helper is null
				RunOnUiThread(() =>
				{
					Toast.MakeText(this, "Failed to get service helper. Cannot open Intent.", ToastLength.Long);
				});

				return;
			}

			// open page to view protocol if a protocol was passed to us
			if (intent.Data != null)
			{
				try
				{
					global::Android.Net.Uri dataURI = intent.Data;

					Protocol protocol = null;

					if (intent.Scheme == "https")
					{
						protocol = await Protocol.DeserializeAsync(new Uri(dataURI.ToString()), true);
					}
					else if (intent.Scheme == "content" || intent.Scheme == "file")
					{
						byte[] bytes = null;

						try
						{
							MemoryStream memoryStream = new MemoryStream();
							Stream inputStream = ContentResolver.OpenInputStream(dataURI);
							inputStream.CopyTo(memoryStream);
							inputStream.Close();
							bytes = memoryStream.ToArray();
						}
						catch (Exception ex)
						{
							throw new Exception("Failed to read bytes from local file URI \"" + dataURI + "\":  " + ex.Message);
						}

						if (bytes != null)
						{
							protocol = await Protocol.DeserializeAsync(bytes, true);
						}
					}
					else
					{
						throw new Exception("Sensus didn't know what to do with URI \"" + dataURI + "\".");
					}

					await Protocol.DisplayAndStartAsync(protocol);
				}
				catch (Exception ex)
				{
					SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
					{
						string message = "Failed to start study:  " + ex.Message;
						new AlertDialog.Builder(this).SetTitle("Error").SetMessage(message).Show();
						SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
					});
				}
			}
			else
			{
				string userResponseAction = intent.GetStringExtra(Notifier.NOTIFICATION_USER_RESPONSE_ACTION_KEY);
				string userResponseMessage = intent.GetStringExtra(Notifier.NOTIFICATION_USER_RESPONSE_MESSAGE_KEY);
				await SensusContext.Current.Notifier.OnNotificationUserResponseAsync(userResponseAction, userResponseMessage);
			}
		}

		#endregion

		#region activity results

		public void GetActivityResultAsync(Intent intent, AndroidActivityResultRequestCode requestCode, Action<Tuple<Result, Intent>> callback)
		{
			Task.Run(() =>
			{
				lock (_locker)
				{
					_activityResultRequestCode = requestCode;
					_activityResult = null;

					_activityResultWait.Reset();

					try
					{
						StartActivityForResult(intent, (int)requestCode);
					}
					catch (Exception ex)
					{
						SensusException.Report(ex);
						_activityResultWait.Set();
					}

					_activityResultWait.WaitOne();

					callback(_activityResult);
				}
			});
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);

			if (requestCode == (int)_activityResultRequestCode)
			{
				_activityResult = new Tuple<Result, Intent>(resultCode, data);
				_activityResultWait.Set();
			}
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
		{
			//PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
			ZXing.Net.Mobile.Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);

			Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}

		#endregion
	}
}