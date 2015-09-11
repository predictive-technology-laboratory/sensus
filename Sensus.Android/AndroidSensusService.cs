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
using Android.OS;
using SensusService;
using Xamarin.Geolocation;
using System;

namespace Sensus.Android
{
    [Service]
    public class AndroidSensusService : Service
    {        
        private AndroidSensusServiceHelper _serviceHelper;

        public override void OnCreate()
        {
            base.OnCreate();

            SensusServiceHelper.Initialize(() => new AndroidSensusServiceHelper());

            _serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

            // it's happened that the service is created after the service helper is disposed:  https://insights.xamarin.com/app/Sensus-Production/issues/46
            if (_serviceHelper == null)
            {
                StopSelf();
                return;
            }
            
            _serviceHelper.SetService(this);
            _serviceHelper.UpdateApplicationStatus("0 protocols are running");
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            _serviceHelper.Logger.Log("Sensus service received start command (startId=" + startId + ").", LoggingLevel.Normal, GetType());

            _serviceHelper.MainActivityWillBeSet = intent.GetBooleanExtra(AndroidSensusServiceHelper.MAIN_ACTIVITY_WILL_BE_SET, false);

            // the service can be stopped without destroying the service object. in such cases, 
            // subsequent calls to start the service will not call OnCreate, which is why the 
            // following code needs to run here -- e.g., starting the helper object and displaying
            // the notification. therefore, it's important that any code called here is
            // okay to call multiple times, even if the service is running. calling this when
            // the service is running can happen because sensus receives a signal on device
            // boot and for any callback alarms that are requested. furthermore, all calls here
            // should be nonblocking / async so we don't tie up the UI thread.

            _serviceHelper.StartAsync(() =>
                {
                    if (intent.GetBooleanExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_KEY, false))
                    {
                        string callbackId = intent.GetStringExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_ID_KEY);
                        if (callbackId != null)
                        {
                            bool repeating = intent.GetBooleanExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_REPEATING_KEY, false);
                            _serviceHelper.RaiseCallbackAsync(callbackId, repeating, true);
                        }
                    }
                });

            return StartCommandResult.RedeliverIntent;
        }

        public override IBinder OnBind(Intent intent)
        {
            return new AndroidSensusServiceBinder(_serviceHelper);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (_serviceHelper != null)
                _serviceHelper.Dispose();
        }
    }
}