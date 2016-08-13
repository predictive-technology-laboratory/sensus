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
using SensusUI;
using Xamarin.Forms;

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

            // we might have failed to create the service helper. it's also happened that the service is created after the 
            // service helper is disposed:  https://insights.xamarin.com/app/Sensus-Production/issues/46
            if (serviceHelper == null)
                Stop();
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

                // acquire wake lock before this method returns to ensure that the device does not sleep prematurely, interrupting the execution of a callback.
                serviceHelper.KeepDeviceAwake();

                // the service can be stopped without destroying the service object. in such cases, 
                // subsequent calls to start the service will not call OnCreate. therefore, it's 
                // important that any code called here is okay to call multiple times, even if the 
                // service is running. calling this when the service is running can happen because 
                // sensus receives a signal on device boot and for any callback alarms that are 
                // requested. furthermore, all calls here should be nonblocking / async so we don't 
                // tie up the UI thread.
                serviceHelper.StartAsync(() =>
                {
                    if (intent != null)
                    {
                        // is this a callback intent?
                        if (intent.GetBooleanExtra(SensusServiceHelper.SENSUS_CALLBACK_KEY, false))
                        {
                            string callbackId = intent.GetStringExtra(SensusServiceHelper.SENSUS_CALLBACK_ID_KEY);

                            // if the user removes the main activity from the switcher, the service's process will be killed and restarted without notice, and 
                            // we'll have no opportunity to unschedule repeating callbacks. when the service is restarted we'll reinitialize the service
                            // helper, restart the repeating callbacks, and we'll then have duplicate repeating callbacks. handle the invalid callbacks below.
                            // if the callback is scheduled, it's fine. if it's not, then unschedule it.
                            if (serviceHelper.CallbackIsScheduled(callbackId))
                            {
                                bool repeating = intent.GetBooleanExtra(SensusServiceHelper.SENSUS_CALLBACK_REPEATING_KEY, false);
                                int repeatDelayMS = intent.GetIntExtra(SensusServiceHelper.SENSUS_CALLBACK_REPEAT_DELAY_KEY, -1);
                                bool repeatLag = intent.GetBooleanExtra(SensusServiceHelper.SENSUS_CALLBACK_REPEAT_LAG_KEY, false);
                                bool wakeLockReleased = false;

                                // raise callback and notify the user if there is a message. we wouldn't have presented the user with the message yet.
                                serviceHelper.RaiseCallbackAsync(callbackId, repeating, repeatDelayMS, repeatLag, true,

                                    // schedule a new callback at the given time.
                                    repeatCallbackTime =>
                                    {
                                        serviceHelper.ScheduleCallbackAlarm(serviceHelper.CreateCallbackPendingIntent(intent), callbackId, repeatCallbackTime);
                                    },

                                    // if the callback indicates that it's okay for the device to sleep, release the wake lock now.
                                    () =>
                                    {
                                        wakeLockReleased = true;
                                        serviceHelper.LetDeviceSleep();
                                        serviceHelper.Logger.Log("Wake lock released preemptively for scheduled callback action.", LoggingLevel.Normal, GetType());
                                    },

                                    // release wake lock now if we didn't while the callback action was executing.
                                    () =>
                                    {
                                        if (!wakeLockReleased)
                                        {
                                            serviceHelper.LetDeviceSleep();
                                            serviceHelper.Logger.Log("Wake lock released after scheduled callback action completed.", LoggingLevel.Normal, GetType());
                                        }
                                    });
                            }
                            else
                            {
                                serviceHelper.UnscheduleCallback(callbackId);
                                serviceHelper.LetDeviceSleep();
                            }
                        }
                        else if (intent.GetStringExtra(AndroidSensusServiceHelper.NOTIFICATION_EXTRA_ID) == AndroidMainActivity.PENDING_SURVEY_NOTIFICATION_ID)
                        {
                            serviceHelper.BringToForeground();

                            Device.BeginInvokeOnMainThread(async () =>
                            {
                                await Xamarin.Forms.Application.Current.MainPage.Navigation.PushAsync(new PendingScriptsPage());
                                serviceHelper.LetDeviceSleep();
                            });
                        }
                        else
                            serviceHelper.LetDeviceSleep();
                    }
                    else
                        serviceHelper.LetDeviceSleep();
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
            NotifyBindingsOfStop();
            StopSelf();
        }

        private void NotifyBindingsOfStop()
        {
            // let everyone who is bound to the service know that we're going to stop.
            lock (_bindings)
            {
                foreach (AndroidSensusServiceBinder binder in _bindings)
                    if (binder.SensusServiceHelper != null && binder.ServiceStopAction != null)
                    {
                        try
                        {
                            binder.ServiceStopAction();
                        }
                        catch (Exception)
                        {
                        }
                    }

                _bindings.Clear();
            }
        }

        public override void OnDestroy()
        {
            Console.Error.WriteLine("--------------------------- Destroying Service ---------------------------");

            base.OnDestroy();

            AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

            // the service helper will be null if we failed to create it within OnCreate, so first check that. also, 
            // OnDestroy can be called either when the user stops Sensus (in Android) and when the system reclaims
            // the service under memory pressure. in the former case, we'll already have done the notification and 
            // stopping of protocols; however, we have no way to know how we reached OnDestroy, so to cover the latter
            // case we're going to do the notification and stopping again. this will be duplicative in the case where
            // the user has stopped sensus. in sum, anything we do below must be safe to run repeatedly.
            if (serviceHelper != null)
            {
                serviceHelper.Logger.Log("Destroying service.", LoggingLevel.Normal, GetType());
                NotifyBindingsOfStop();
                serviceHelper.StopProtocols();
                serviceHelper.SetService(null);
            }
        }
    }
}