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

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using SensusService;
using SensusUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xamarin.Facebook;
using Xamarin;
using Xam.Plugin.MapExtend.Droid;

[assembly:MetaData("com.facebook.sdk.ApplicationId", Value = "@string/app_id")]

namespace Sensus.Android
{
    [Activity(Label = "@string/app_name", MainLauncher = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "http", DataHost = "*", DataPathPattern = ".*\\\\.json")]  // protocols downloaded from an http web link
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "https", DataHost = "*", DataPathPattern = ".*\\\\.json")]  // protocols downloaded from an https web link
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault }, DataMimeType = "application/json")]  // protocols obtained from "file" and "content" schemes:  http://developer.android.com/guide/components/intents-filters.html#DataTest
    public class AndroidMainActivity : FormsApplicationActivity
    {
        private static readonly string INPUT_REQUESTED_NOTIFICATION_ID = "INPUT-REQUESTED-NOTIFICATION-ID";

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

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Console.Error.WriteLine("--------------------------- Creating activity ---------------------------");

            base.OnCreate(savedInstanceState);

            SensusServiceHelper.Initialize(() => new AndroidSensusServiceHelper());

            _activityResultWait = new ManualResetEvent(false);
            _facebookCallbackManager = CallbackManagerFactory.Create();
            _serviceBindWait = new ManualResetEvent(false);

            Window.AddFlags(global::Android.Views.WindowManagerFlags.DismissKeyguard);
            Window.AddFlags(global::Android.Views.WindowManagerFlags.ShowWhenLocked);
            Window.AddFlags(global::Android.Views.WindowManagerFlags.TurnScreenOn);

            Forms.Init(this, savedInstanceState);
            FormsMaps.Init(this, savedInstanceState);
            MapExtendRenderer.Init(this, savedInstanceState);

            #if UNIT_TESTING
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

                if (e.Binder.SensusServiceHelper.BarcodeScanner == null)
                {
                    try
                    {
                        e.Binder.SensusServiceHelper.BarcodeScanner = new ZXing.Mobile.MobileBarcodeScanner();
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to create barcode scanner:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                }

                // give service helper a reference to this activity
                e.Binder.SensusServiceHelper.MainActivityWillBeDisplayed = false;
                e.Binder.SensusServiceHelper.SetFocusedMainActivity(this);

                // signal the activity that the service has been bound
                _serviceBindWait.Set();

                // if we're unit testing, try to load and run the unit testing protocol from the embedded assets
                #if UNIT_TESTING
                using (Stream protocolFile = Assets.Open("UnitTestingProtocol.json"))
                {
                    Protocol.RunUnitTestingProtocol(protocolFile);
                    protocolFile.Close();
                }
                #endif
            };

            _serviceConnection.ServiceDisconnected += (o, e) =>
            {
                DisconnectFromService();                
            };

            OpenIntentAsync(Intent);
        }

        protected override void OnStart()
        {
            Console.Error.WriteLine("--------------------------- Starting activity ---------------------------");

            base.OnStart();

            // start service. if the service is already running, this will have no tangible effect. one might consider 
            // starting the service within OnCreate, but putting it in OnStart might be better. if the service is killed 
            // by the Android system, it will need to be restarted before the activity can operate normally. Android 
            // will probably try to restart the service automatically, but we're not exactly sure if/when this will happen.
            Intent serviceIntent = new Intent(this, typeof(AndroidSensusService));
            serviceIntent.PutExtra(AndroidSensusServiceHelper.MAIN_ACTIVITY_WILL_BE_DISPLAYED, true);
            StartService(serviceIntent);

            // bind the activity to the service
            BindService(serviceIntent, _serviceConnection, Bind.AutoCreate);
        }

        protected override void OnResume()
        {
            Console.Error.WriteLine("--------------------------- Resuming activity ---------------------------");

            base.OnResume();

            (SensusServiceHelper.Get() as AndroidSensusServiceHelper).IssueNotificationAsync("Sensus", null, true, false, INPUT_REQUESTED_NOTIFICATION_ID);

            // we might still be waiting for a connection to the sensus service. prevent the user from interacting with the UI
            // by displaying a progress dialog that cannot be cancelled. keep the dialog up until the service has connected.
            // if the service has already connected, the wait handle below will already be set and the dialog will immediately
            // be dismissed.

            ProgressDialog serviceBindWaitDialog = ProgressDialog.Show(this, "Please Wait", "Binding to Sensus", true, false);

            // start new thread to wait for connection, since we're currently on the UI thread, which the service connection needs in order to complete.
            new Thread(() =>
                {
                    _serviceBindWait.WaitOne();

                    // now that the service connection has been established, dismiss the wait dialog and show protocols.
                    Device.BeginInvokeOnMainThread(() =>
                        {
                            serviceBindWaitDialog.Dismiss();
                            (App.Current as App).ProtocolsPage.Bind();
                        });
                    
                }).Start();
        }

        #region intent handling

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            OpenIntentAsync(intent);
        }

        private void OpenIntentAsync(Intent intent)
        {
            new Thread(() =>
                {
                    // open page to view protocol if a protocol was passed to us
                    if (intent.Data != null)
                    {
                        global::Android.Net.Uri dataURI = intent.Data;

                        try
                        {
                            if (intent.Scheme == "http" || intent.Scheme == "https")
                                Protocol.DeserializeAsync(new Uri(dataURI.ToString()), true, Protocol.DisplayAndStartAsync);
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
                                    Protocol.DeserializeAsync(bytes, true, Protocol.DisplayAndStartAsync);
                            }
                            else
                                SensusServiceHelper.Get().Logger.Log("Sensus didn't know what to do with URI \"" + dataURI + "\".", LoggingLevel.Normal, GetType());
                        }
                        catch (Exception ex)
                        {
                            Device.BeginInvokeOnMainThread(() =>
                                {
                                    new AlertDialog.Builder(this).SetTitle("Failed to get protocol").SetMessage(ex.Message).Show();
                                });
                        }
                    }

                }).Start();
        }

        #endregion

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);

            AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

            if (serviceHelper != null)
            {
                if (hasFocus)
                    serviceHelper.SetFocusedMainActivity(this);
                else
                    serviceHelper.SetFocusedMainActivity(null);
            }
        }

        #region activity results

        public void GetActivityResultAsync(Intent intent, AndroidActivityResultRequestCode requestCode, Action<Tuple<Result, Intent>> callback)
        {
            new Thread(() =>
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
                            Insights.Report(ex, Insights.Severity.Error);
                            _activityResultWait.Set();
                        }

                        _activityResultWait.WaitOne();

                        callback(_activityResult);
                    }

                }).Start();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == (int)_activityResultRequestCode)
            {
                _activityResult = new Tuple<Result, Intent>(resultCode, data);
                _activityResultWait.Set();
            }

            // looks like the facebook SDK can become uninitialized during the process of interacting with the Facebook login manager. this 
            // might happen when Sensus is stopped/destroyed while the user is logging into facebook. check here to ensure that the facebook
            // SDK is initialized.
            //
            // see:  https://insights.xamarin.com/app/Sensus-Production/issues/66
            //
            if (!FacebookSdk.IsInitialized)
                FacebookSdk.SdkInitialize(this);
            
            _facebookCallbackManager.OnActivityResult(requestCode, (int)resultCode, data);
        }

        #endregion

        protected override void OnPause()
        {
            Console.Error.WriteLine("--------------------------- Pausing activity ---------------------------");

            base.OnPause();
                
            // force disconnection between service and main activity
            OnWindowFocusChanged(false);
        }

        protected override void OnStop()
        {
            Console.Error.WriteLine("--------------------------- Stopping activity ---------------------------");

            base.OnStop();

            DisconnectFromService();

            if (SensusServiceHelper.PromptIsRunning)
                (SensusServiceHelper.Get() as AndroidSensusServiceHelper).IssueNotificationAsync("Sensus", "Your input is requested.", true, false, INPUT_REQUESTED_NOTIFICATION_ID);
        }

        private void DisconnectFromService()
        {
            _serviceBindWait.Reset();

            // unbind from service
            if (_serviceConnection.Binder != null)
            {         
                // it's happened that the service is created after the service helper is disposed:  https://insights.xamarin.com/app/Sensus-Production/issues/46
                // binding to the service in such a situation can result in a null service helper within the binder.
                if (_serviceConnection.Binder.SensusServiceHelper != null)
                {    
                    // save the state of the service helper before unbinding
                    _serviceConnection.Binder.SensusServiceHelper.SaveAsync();

                    UnbindService(_serviceConnection);
                }
            }
        }
    }
}