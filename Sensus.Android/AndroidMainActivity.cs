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
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "http", DataHost = "*", DataPathPattern = ".*\\\\.sensus")]  // protocols downloaded from an http web link
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "https", DataHost = "*", DataPathPattern = ".*\\\\.sensus")]  // protocols downloaded from an https web link
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault }, DataMimeType = "*/*", DataScheme = "content", DataHost = "*")]  // protocols opened from email attachments originating from the sensus app itself -- DataPathPattern doesn't work here, since email apps (e.g., gmail) rename attachments when stored in the local file system
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault }, DataMimeType = "*/*", DataScheme = "file", DataHost = "*", DataPathPattern = ".*\\\\.sensus")]  // protocols opened from the local file system
    public class AndroidMainActivity : FormsApplicationActivity
    {
        public event EventHandler Stopped;

        private AndroidSensusServiceConnection _serviceConnection;
        private ManualResetEvent _activityResultWait;
        private AndroidActivityResultRequestCode _activityResultRequestCode;
        private Tuple<Result, Intent> _activityResult;
        private ManualResetEvent _uiReadyWait;
        private ICallbackManager _facebookCallbackManager;
        private App _app;
        private bool _isForegrounded;

        private readonly object _locker = new object();

        public ManualResetEvent UiReadyWait
        {
            get { return _uiReadyWait; }
        }

        public bool IsForegrounded
        {
            get
            {
                return _isForegrounded;
            }
        }

        public ICallbackManager FacebookCallbackManager
        {
            get { return _facebookCallbackManager; }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SensusServiceHelper.Initialize(() => new AndroidSensusServiceHelper());

            _uiReadyWait = new ManualResetEvent(false);
            _activityResultWait = new ManualResetEvent(false);
            _facebookCallbackManager = CallbackManagerFactory.Create();
            _isForegrounded = false;

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
                // binding to the service in such a situation can result in a null service helper within the binder.
                if (e.Binder.SensusServiceHelper == null)
                {
                    Finish();
                    return;
                }
                    
                // get reference to service helper for use within the UI
                UiBoundSensusServiceHelper.Set(e.Binder.SensusServiceHelper);

                // give service helper a reference to this activity
                e.Binder.SensusServiceHelper.MainActivityWillBeSet = false;
                e.Binder.SensusServiceHelper.SetMainActivity(this);

                // display service helper properties on the main page
                _app.SensusMainPage.DisplayServiceHelper(e.Binder.SensusServiceHelper);

                // if we're unit testing, try to load up the unit testing protocol
                #if UNIT_TESTING
                try
                {
                    using (Stream protocolFile = Assets.Open("UnitTestingProtocol.sensus"))
                    using (MemoryStream protocolStream = new MemoryStream())
                    {
                        protocolFile.CopyTo(protocolStream);
                        protocolFile.Close();
                        byte[] protocolBytes = protocolStream.ToArray();
                        protocolStream.Close();
                        Protocol.DisplayFromBytesAsync(protocolBytes);
                    }
                }
                catch (Exception ex)
                {
                    string message = "Failed to open unit testing protocol:  " + ex.Message;
                    SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());
                    throw new Exception(message);
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
            base.OnStart();

            _isForegrounded = true;
        }

        protected override void OnResume()
        {
            base.OnResume();

            // start service -- if it's already running, this will have no effect
            Intent serviceIntent = new Intent(this, typeof(AndroidSensusService));
            serviceIntent.PutExtra(AndroidSensusServiceHelper.MAIN_ACTIVITY_WILL_BE_SET, true);
            StartService(serviceIntent);

            // bind to service
            BindService(serviceIntent, _serviceConnection, Bind.AutoCreate);
        }

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
                                Protocol.DisplayFromWebUriAsync(new Uri(dataURI.ToString()));
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
                                    Protocol.DisplayFromBytesAsync(bytes);
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

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);

            if (hasFocus)
                _uiReadyWait.Set();
            else
                _uiReadyWait.Reset();
        }

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

        protected override void OnPause()
        {
            base.OnPause();
                
            // reset the UI ready wait handle            
            OnWindowFocusChanged(false);

            DisconnectFromService();
        }

        protected override void OnStop()
        {
            base.OnStop();

            _isForegrounded = false;

            if (SensusServiceHelper.Get() != null)
                SensusServiceHelper.Get().Logger.Log("Stopping main activity.", LoggingLevel.Normal, GetType());            

            if (Stopped != null)
                Stopped(this, null);
        }

        private void DisconnectFromService()
        {
            // remove service helper from UI
            _app.SensusMainPage.RemoveServiceHelper();

            // make service helper inaccessible to UI
            UiBoundSensusServiceHelper.Set(null);

            // unbind from service
            if (_serviceConnection.Binder != null)
            {         
                // it's happened that the service is created after the service helper is disposed:  https://insights.xamarin.com/app/Sensus-Production/issues/46
                // binding to the service in such a situation can result in a null service helper within the binder.
                if (_serviceConnection.Binder.SensusServiceHelper != null)
                {    
                    _serviceConnection.Binder.SensusServiceHelper.SetMainActivity(null);
                    _serviceConnection.Binder.SensusServiceHelper.SaveAsync();
                }

                if (_serviceConnection.Binder.IsBound)
                    UnbindService(_serviceConnection);
            }
        }
    }
}