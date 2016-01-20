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
    /// <summary>
    /// Android sensus service. Manages background running of Sensus. This is a hybrid service (http://developer.android.com/guide/components/services.html), in that
    /// it is started by the Sensus activity to run indefinitely, but the activity also binds to it to manage the Sensus system (e.g., creating protocols, starting
    /// and stopping them, etc. For now, nobody other than the Sensus activity can interact with the service (Exported = false). Perhaps we'll allow this in the future
    /// to support integration with other apps.
    /// </summary>
    [Service(Exported = false)]
    public class AndroidSensusService : Service
    {
        public override void OnCreate()
        {
            base.OnCreate();

            SensusServiceHelper.Initialize(() => new AndroidSensusServiceHelper());

            AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

            // it's happened that the service is created after the service helper is disposed:  https://insights.xamarin.com/app/Sensus-Production/issues/46
            if (serviceHelper == null)
            {
                StopSelf();
                return;
            }
            
            serviceHelper.SetService(this);
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

            serviceHelper.Logger.Log("Sensus service received start command (startId=" + startId + ").", LoggingLevel.Normal, GetType());

            serviceHelper.MainActivityWillBeDisplayed = intent.GetBooleanExtra(AndroidSensusServiceHelper.MAIN_ACTIVITY_WILL_BE_DISPLAYED, false);

            // the service can be stopped without destroying the service object. in such cases, 
            // subsequent calls to start the service will not call OnCreate. therefore, it's 
            // important that any code called here is okay to call multiple times, even if the 
            // service is running. calling this when the service is running can happen because 
            // sensus receives a signal on device boot and for any callback alarms that are 
            // requested. furthermore, all calls here should be nonblocking / async so we don't 
            // tie up the UI thread.

            serviceHelper.StartAsync(() =>
                {
                    if (intent.GetBooleanExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_KEY, false))
                    {
                        string callbackId = intent.GetStringExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_ID_KEY);
                        if (callbackId != null)
                        {
                            bool repeating = intent.GetBooleanExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_REPEATING_KEY, false);
                            serviceHelper.RaiseCallbackAsync(callbackId, repeating, true);
                        }
                    }
                });

            return StartCommandResult.RedeliverIntent;
        }

        public override IBinder OnBind(Intent intent)
        {
            return new AndroidSensusServiceBinder(SensusServiceHelper.Get() as AndroidSensusServiceHelper);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

            if (serviceHelper != null)
            {
                serviceHelper.Logger.Log("Destroying service.", LoggingLevel.Normal, GetType());
                serviceHelper.Dispose();
            }
        }
    }
}