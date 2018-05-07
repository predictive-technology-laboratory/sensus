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
using Xamarin.Facebook;
using Xamarin.Forms.Platform.Android;
using Plugin.CurrentActivity;
using System.Threading.Tasks;
using Sensus.Exceptions;
using Android.Gms.Common;
using Firebase.Iid;
using Firebase.Messaging;

#if __ANDROID_23__
using Plugin.Permissions;
#endif

[assembly: MetaData("com.facebook.sdk.ApplicationId", Value = "@string/app_id")]
[assembly: UsesPermission(Microsoft.Band.BandClientManager.BindBandService)]

namespace Sensus.Android
{
    [Activity(Label = "@string/app_name", MainLauncher = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "http", DataHost = "*", DataPathPattern = ".*\\\\.json")]  // protocols downloaded from an http web link
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "https", DataHost = "*", DataPathPattern = ".*\\\\.json")]  // protocols downloaded from an https web link
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault }, DataMimeType = "application/json")]  // protocols obtained from "file" and "content" schemes:  http://developer.android.com/guide/components/intents-filters.html#DataTest
    public class AndroidMainActivity : FormsApplicationActivity
    {
        private AndroidSensusServiceConnection _serviceConnection;
        private ManualResetEvent _activityResultWait;
        private AndroidActivityResultRequestCode _activityResultRequestCode;
        private Tuple<Result, Intent> _activityResult;
        private ICallbackManager _facebookCallbackManager;
        private App _app;
        private ManualResetEvent _serviceBindWait;

        private readonly object _locker = new object();

        public ICallbackManager FacebookCallbackManager
        {
            get { return _facebookCallbackManager; }
        }

        public bool IsPlayServicesAvailable()
        {
            int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            if (resultCode != ConnectionResult.Success)
            {
                if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
                {
                    // note: you can give the user a chance to fix the issue.
                    Console.WriteLine($"Error: {GoogleApiAvailability.Instance.GetErrorString(resultCode)}");
                }
                else
                {
                    Console.WriteLine("Error: Play services not supported!");
                    Finish();
                }
                return false;
            }
            else
            {
                Console.WriteLine("Play Services available.");
                return true;
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Console.Error.WriteLine("--------------------------- Creating activity ---------------------------");

            base.OnCreate(savedInstanceState);

            _activityResultWait = new ManualResetEvent(false);
            _facebookCallbackManager = CallbackManagerFactory.Create();
            _serviceBindWait = new ManualResetEvent(false);

            Window.AddFlags(global::Android.Views.WindowManagerFlags.DismissKeyguard);
            Window.AddFlags(global::Android.Views.WindowManagerFlags.ShowWhenLocked);
            Window.AddFlags(global::Android.Views.WindowManagerFlags.TurnScreenOn);

            Forms.Init(this, savedInstanceState);
            FormsMaps.Init(this, savedInstanceState);
            ZXing.Net.Mobile.Forms.Android.Platform.Init();
            CrossCurrentActivity.Current.Activity = this;

#if UI_TESTING
            Forms.ViewInitialized += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.View.StyleId))
                    e.NativeView.ContentDescription = e.View.StyleId;
            };
#endif

            _app = new App();
            LoadApplication(_app);

            _serviceConnection = new AndroidSensusServiceConnection();
            _serviceConnection.ServiceConnected += (o, e) =>
            {
                // it's happened that the service is created / started after the service helper is disposed:  https://insights.xamarin.com/app/Sensus-Production/issues/46
                // binding to the service in such a situation can result in a null service helper within the binder. if the service helper was disposed, then the goal is 
                // to close down sensus. so finish the activity.
                if (e.Binder.SensusServiceHelper == null)
                {
                    Finish();
                    return;
                }

                // tell the service to finish this activity when it is stopped
                e.Binder.ServiceStopAction = Finish;

                // signal the activity that the service has been bound
                _serviceBindWait.Set();

                // if we're UI testing, try to load and run the UI testing protocol from the embedded assets
#if UI_TESTING
                using (Stream protocolFile = Assets.Open("UiTestingProtocol.json"))
                {
                    Protocol.RunUiTestingProtocol(protocolFile);
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

            OpenIntentAsync(Intent);

            // detect connection with Google Play services, finishes if not able to connect.
            IsPlayServicesAvailable();
#if DEBUG
            // on debugging, a redeploy of the app will delete the old token and effectively force a token refresh
            Task.Run(() =>
            {
                Console.WriteLine("DEBUG: deleting prior token: " + FirebaseInstanceId.Instance.Token);
                FirebaseInstanceId.Instance.DeleteInstanceId();
            });
#endif
        }

        protected override void OnStart()
        {
            Console.Error.WriteLine("--------------------------- Starting activity ---------------------------");

            base.OnStart();

            CrossCurrentActivity.Current.Activity = this;
        }

        protected override void OnResume()
        {
            Console.Error.WriteLine("--------------------------- Resuming activity ---------------------------");

            base.OnResume();

            CrossCurrentActivity.Current.Activity = this;

            // temporarily disable UI while we bind to service
            Window.AddFlags(global::Android.Views.WindowManagerFlags.NotTouchable);

            // make sure that the service is running and bound any time the activity is resumed. the service is both started
            // and bound, as we'd like the service to remain running and available to other apps even if the current activity unbinds.
            Intent serviceIntent = AndroidSensusService.StartService(this);
            BindService(serviceIntent, _serviceConnection, Bind.AboveClient);

            // start new task to wait for connection, since we're currently on the UI thread, which the service connection needs in order to complete.
            Task.Run(() =>
            {
                // we've not seen the binding take more than a second or two; however, we want to be very careful not to block indefinitely
                // here becaus the UI is currently disabled. if for some strange reason the binding does not work, bail out after 10 seconds
                // and let the user interact with the UI. most likely, a crash will be coming very soon in this case, as the sensus service
                // will probably not be running. again, this has not occurred in practice, but allowing the crash to occur will send us information
                // through the crash analytics service and we'll be able to track it
                _serviceBindWait.WaitOne(TimeSpan.FromSeconds(10000));

                SensusServiceHelper.Get().ClearPendingSurveysNotificationAsync();

                // now that the service connection has been established, reenable UI.
                try
                {
                    SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                    {
                        Window.ClearFlags(global::Android.Views.WindowManagerFlags.NotTouchable);
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

        protected override void OnStop()
        {
            Console.Error.WriteLine("--------------------------- Stopping activity ---------------------------");

            base.OnStop();

            SensusServiceHelper serviceHelper = SensusServiceHelper.Get();

            if (serviceHelper != null)
            {
                serviceHelper.Save();
                serviceHelper.IssuePendingSurveysNotificationAsync(null, true);
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
                _serviceConnection.Binder.ServiceStopAction = null;
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
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to disconnection from service:  " + ex.Message, LoggingLevel.Normal, GetType());
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
        [Service]
        [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
        public class MyFirebaseIIDService : FirebaseInstanceIdService
        {
            const string TAG = "MyFirebaseIIDService";
            public override void OnTokenRefresh()
            {
                var refreshedToken = FirebaseInstanceId.Instance.Token;
                Console.WriteLine("Refreshed token, new val: " + refreshedToken);
                SendRegistrationToServer(refreshedToken);
            }
            void SendRegistrationToServer(string token)
            {
                // Add custom implementation, as needed.
            }
        }

        [Service]
        [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
        public class MyFirebaseMessagingService : FirebaseMessagingService
        {
            public override void OnMessageReceived(RemoteMessage message)
            {
                try
                {
                    // log for testing
                    var from = message.From;
                    var body = message.GetNotification().Body;
                    Console.WriteLine("From: " + from);
                    Console.WriteLine("Notification Message Body: " + body);
                }
                catch (Exception ex)
                {
                    // TODO : better error handling
                    Console.WriteLine("Error extracting message from notification: " + ex);
                }
            }
        }
        #region intent handling

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            OpenIntentAsync(intent);
        }

        private Task OpenIntentAsync(Intent intent)
        {
            return Task.Run(() =>
            {
                // wait for service helper to be initialized, since this method might be called before the service starts up
                // and initializes the service helper.
                int timeToWaitMS = 60000;
                int waitIntervalMS = 1000;
                while (SensusServiceHelper.Get() == null && timeToWaitMS > 0)
                {
                    Thread.Sleep(waitIntervalMS);
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
                    global::Android.Net.Uri dataURI = intent.Data;

                    try
                    {
                        if (intent.Scheme == "http" || intent.Scheme == "https")
                        {
                            Protocol.DeserializeAsync(new Uri(dataURI.ToString()), Protocol.DisplayAndStartAsync);
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
                                SensusServiceHelper.Get().Logger.Log("Failed to read bytes from local file URI \"" + dataURI + "\":  " + ex.Message, LoggingLevel.Normal, GetType());
                            }

                            if (bytes != null)
                            {
                                Protocol.DeserializeAsync(bytes, Protocol.DisplayAndStartAsync);
                            }
                        }
                        else
                        {
                            SensusServiceHelper.Get().Logger.Log("Sensus didn't know what to do with URI \"" + dataURI + "\".", LoggingLevel.Normal, GetType());
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                        {
                            new AlertDialog.Builder(this).SetTitle("Failed to get protocol").SetMessage(ex.Message).Show();
                        });
                    }
                }
            });
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

            _facebookCallbackManager.OnActivityResult(requestCode, (int)resultCode, data);
        }

#if __ANDROID_23__
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            ZXing.Net.Mobile.Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
#endif

#endregion
    }
}