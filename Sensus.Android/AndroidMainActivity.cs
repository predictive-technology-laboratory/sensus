#region copyright
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
#endregion
 
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using SensusService;
using SensusService.Exceptions;
using SensusUI;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace Sensus.Android
{
    [Activity(Label = "ensus", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "http", DataHost = "*", DataPathPattern = ".*\\\\.sensus")]  // protocols downloaded from an http web link
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "https", DataHost = "*", DataPathPattern = ".*\\\\.sensus")]  // protocols downloaded from an https web link
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault }, DataMimeType = "application/octet-stream", DataScheme = "content", DataHost = "*")]  // protocols opened from email attachments originating from the sensus app itself -- DataPathPattern doesn't work here, since email apps (e.g., gmail) rename attachments when stored in the local file system
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault }, DataMimeType = "text/plain", DataScheme = "content", DataHost = "*")]  // protocols opened from email attachments originating from non-sensus senders (i.e., the "share" button in sensus) -- DataPathPattern doesn't work here, since email apps (e.g., gmail) rename attachments when stored in the local file system
    [IntentFilter(new string[] { Intent.ActionView }, Categories = new string[] { Intent.CategoryDefault }, DataMimeType = "text/plain", DataScheme = "file", DataHost = "*", DataPathPattern = ".*\\\\.sensus")]  // protocols opened from the local file system
    public class AndroidMainActivity : AndroidActivity
    {
        private AndroidSensusServiceConnection _serviceConnection;
        private ManualResetEvent _activityResultWait;
        private AndroidActivityResultRequestCode _activityResultRequestCode;
        private Tuple<Result, Intent> _activityResult;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Forms.Init(this, bundle);

            _activityResultWait = new ManualResetEvent(false);

            // start service -- if it's already running, this will have no effect
            Intent serviceIntent = new Intent(this, typeof(AndroidSensusService));
            StartService(serviceIntent);

            // bind UI to the service
            _serviceConnection = new AndroidSensusServiceConnection(null);
            _serviceConnection.ServiceConnected += async (o, e) =>
                {
                    // before binding, add reference to main activity within the service helper
                    e.Binder.SensusServiceHelper.MainActivity = this;

                    // get reference to service helper for use within the UI
                    UiBoundSensusServiceHelper.Set(e.Binder.SensusServiceHelper);
                    UiBoundSensusServiceHelper.Get().Stopped += (oo, ee) => { Finish(); };  // stop activity when service stops    

                    // display main page
                    SensusNavigationPage navigationPage = new SensusNavigationPage(UiBoundSensusServiceHelper.Get());
                    SetPage(navigationPage);

                    #region open page to view protocol if a protocol was passed to us
                    if (Intent.Data != null)
                    {
                        global::Android.Net.Uri dataURI = Intent.Data;

                        Protocol protocol = null;
                        try
                        {
                            if (Intent.Scheme == "http" || Intent.Scheme == "https")
                                protocol = Protocol.GetFromWebURI(new Uri(dataURI.ToString()));
                            else if (Intent.Scheme == "content" || Intent.Scheme == "file")
                            {
                                Stream stream = null;

                                try { stream = ContentResolver.OpenInputStream(dataURI); }
                                catch (Exception ex) { throw new SensusException("Failed to open local protocol file URI \"" + dataURI + "\":  " + ex.Message); }

                                if (stream != null)
                                    protocol = Protocol.GetFromStream(stream);
                            }
                            else
                                throw new SensusException("Sensus didn't know what to do with URI \"" + dataURI);
                        }
                        catch (Exception ex) { new AlertDialog.Builder(this).SetTitle("Failed to get protocol").SetMessage(ex.Message).Show(); }

                        if (protocol != null)
                        {
                            try
                            {
                                UiBoundSensusServiceHelper.Get().RegisterProtocol(protocol);
                                await navigationPage.PushAsync(new ProtocolPage(protocol));
                            }
                            catch (Exception ex)
                            {
                                string message = "Failed to register/display new protocol:  " + ex.Message;
                                SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal);
                                new AlertDialog.Builder(this).SetTitle("Failed to show protocol").SetMessage(message).Show();
                            }
                        }
                    }
                    #endregion
                };

            BindService(serviceIntent, _serviceConnection, Bind.AutoCreate);
        }

        public Task<Tuple<Result, Intent>> GetActivityResultAsync(Intent intent, AndroidActivityResultRequestCode requestCode)
        {
            return Task.Run<Tuple<Result, Intent>>(() =>
                {
                    lock (this)
                    {
                        _activityResultRequestCode = requestCode;

                        _activityResultWait.Reset();
                        StartActivityForResult(intent, (int)requestCode);
                        _activityResultWait.WaitOne();

                        return _activityResult;
                    }
                });
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == (int)_activityResultRequestCode)
            {
                _activityResult = new Tuple<Result, Intent>(resultCode, data);
                _activityResultWait.Set();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_serviceConnection.Binder.IsBound)
                UnbindService(_serviceConnection);
        }
    }
}