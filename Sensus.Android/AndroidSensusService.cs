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
using System;
using System.Collections.Generic;

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
        private List<AndroidSensusServiceBinder> _bindings;

        public override void OnCreate()
        {
            base.OnCreate();

            _bindings = new List<AndroidSensusServiceBinder>();

            SensusServiceHelper.Initialize(() => new AndroidSensusServiceHelper());

            AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

            // it's happened that the service is created after the service helper is disposed:  https://insights.xamarin.com/app/Sensus-Production/issues/46
            if (serviceHelper == null)
                StopSelf();
            else
                serviceHelper.SetService(this);
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

            // there might be a race condition between the calling of this method and the stopping/disposal of the service helper.
            // if the service helper is stopped/disposed before the service is stopped but after this method is called, the service
            // helper will be null.
            if (serviceHelper != null)
            {
                serviceHelper.Logger.Log("Sensus service received start command (startId=" + startId + ", flags=" + flags + ").", LoggingLevel.Normal, GetType());

                // the service can be stopped without destroying the service object. in such cases, 
                // subsequent calls to start the service will not call OnCreate. therefore, it's 
                // important that any code called here is okay to call multiple times, even if the 
                // service is running. calling this when the service is running can happen because 
                // sensus receives a signal on device boot and for any callback alarms that are 
                // requested. furthermore, all calls here should be nonblocking / async so we don't 
                // tie up the UI thread.
                serviceHelper.StartAsync(() =>
                    {         
                        // is this a callback intent?
                        if (intent != null && intent.GetBooleanExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_KEY, false))
                        {
                            string callbackId = intent.GetStringExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_ID_KEY);

                            // if the user removes the main activity from the switcher, the service's process will be killed and restarted without notice, and 
                            // we'll have no opportunity to unschedule repeating callbacks. when the service is restarted we'll reinitialize the service
                            // helper, restart the repeating callbacks, and we'll then have duplicate repeating callbacks. handle the invalid callbacks below.
                            // if the callback is scheduled, it's fine. if it's not, then unschedule it.
                            if (serviceHelper.CallbackIsScheduled(callbackId))
                            {
                                bool repeating = intent.GetBooleanExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_REPEATING_KEY, false);
                                int repeatDelayMS = intent.GetIntExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_REPEAT_DELAY_KEY, -1);
                                bool repeatLag = intent.GetBooleanExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_REPEAT_LAG_KEY, false);

                                // raise callback and notify the user if there is a message. we wouldn't have presented the user with the message yet.
                                serviceHelper.RaiseCallbackAsync(callbackId, repeating, repeatDelayMS, repeatLag, true, repeatCallbackTime =>
                                    {
                                        PendingIntent callbackPendingIntent = PendingIntent.GetService(this, callbackId.GetHashCode(), intent, PendingIntentFlags.CancelCurrent);
                                        serviceHelper.ScheduleCallbackAlarm(callbackPendingIntent, repeatCallbackTime);
                                    });
                            }
                            else
                                serviceHelper.UnscheduleCallback(callbackId);
                        }
                    });
            }

            return StartCommandResult.Sticky;
        }

        public override IBinder OnBind(Intent intent)
        {
            AndroidSensusServiceBinder binder = new AndroidSensusServiceBinder(SensusServiceHelper.Get() as AndroidSensusServiceHelper);

            lock (_bindings)
                _bindings.Add(binder);
            
            return binder;
        }

        public void Stop()
        {
            try
            {
                StopSelf();
            }
            catch (Exception)
            {
            }

            lock (_bindings)
            {
                foreach (AndroidSensusServiceBinder binder in _bindings)
                    if (binder.ServiceStopAction != null)
                    {
                        try
                        {
                            binder.ServiceStopAction();
                        }
                        catch (Exception)
                        {
                        }
                    }
            }
        }

        public override void OnDestroy()
        {
            Console.Error.WriteLine("--------------------------- Destroying Service ---------------------------");

            base.OnDestroy();

            // ondestroy can be called in two different ways. first, the user might stop sensus from within the activity. if this
            // happens, the service helper will be stopped/disposed, and the service will be stopped. at some indeterminate point 
            // in the future, the service will be destroyed and cleaned up, calling this method. in this case we don't need to do 
            // anything since all of the stopping/cleaning has already happened. this case is indicated by a null service helper. 
            // the other way we might find ourselves in ondestroy is if system resources are running out and android decides to 
            // reclaim the service. in this case we need to stop/dispose the service helper. this case is indicated by a non-null 
            // service helper.
           
            AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

            if (serviceHelper != null)
            {
                // case 2 applies (see above)
                serviceHelper.Logger.Log("Destroying service.", LoggingLevel.Normal, GetType());
                serviceHelper.Stop();
                serviceHelper.SetService(null);
            }
        }
    }
}