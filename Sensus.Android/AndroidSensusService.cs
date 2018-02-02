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
using System;
using System.Collections.Generic;
using Sensus.Context;
using Sensus.Android.Context;
using Sensus.Callbacks;
using Sensus.Android.Callbacks;
using Sensus.Android.Concurrent;
using Sensus.Encryption;
using System.Linq;
using System.Threading.Tasks;

namespace Sensus.Android
{
    /// <summary>
    /// Android sensus service. Manages background running of Sensus. This is a hybrid service (http://developer.android.com/guide/components/services.html), in that
    /// it is started by the Sensus activity to run indefinitely, but the activity also binds to it to manage the Sensus system (e.g., creating protocols, starting
    /// and stopping them, etc.). For now, nobody other than the Sensus activity can interact with the service (Exported = false). Perhaps we'll allow this in the future
    /// to support integration with other apps.
    /// </summary>
    [Service(Exported = false, Label = "Runs the Sensus mobile sensing application.")]
    public class AndroidSensusService : Service
    {
        public static Intent StartService(global::Android.Content.Context context)
        {
            Intent serviceIntent = new Intent(context, typeof(AndroidSensusService));

            // after android 26, starting a foreground service requires the use of StartForegroundService rather than StartService.
            // in either case, the service itself will call StartForeground after it has started. more info:  
            // 
            //     https://developer.android.com/reference/android/content/Context.html#startForegroundService(android.content.Intent)
            //
            // also see notes on backwards compatibility for how the compiler directives are used below:
            //
            //    https://github.com/predictive-technology-laboratory/sensus/wiki/Backwards-Compatibility
#if __ANDROID_26__
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                context.StartForegroundService(serviceIntent);
            }
            else
#endif
            {
                context.StartService(serviceIntent);
            }

            return serviceIntent;
        }

        public const int FOREGROUND_SERVICE_NOTIFICATION_ID = 1;

        private readonly List<AndroidSensusServiceBinder> _bindings = new List<AndroidSensusServiceBinder>();
        private Notification.Builder _foregroundServiceNotificationBuilder;

        public override void OnCreate()
        {
            base.OnCreate();

            SensusContext.Current = new AndroidSensusContext
            {
                Platform = Platform.Android,
                MainThreadSynchronizer = new MainConcurrent(),
                SymmetricEncryption = new SymmetricEncryption(SensusServiceHelper.ENCRYPTION_KEY),
                CallbackScheduler = new AndroidCallbackScheduler(this),
                Notifier = new AndroidNotifier(this)
            };

            SensusServiceHelper.Initialize(() => new AndroidSensusServiceHelper());

            AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

            // we might have failed to create the service helper. it's also happened that the service is created after the 
            // service helper is disposed:  https://insights.xamarin.com/app/Sensus-Production/issues/46
            if (serviceHelper == null)
            {
                Stop();
            }
            else
            {
                serviceHelper.SetService(this);
            }
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

            // there might be a race condition between the calling of this method and the stopping/disposal of the service helper.
            // if the service helper is stopped/disposed before the service is stopped but after this method is called (e.g., by
            // an alarm callback), the service helper will be null.
            if (serviceHelper != null)
            {
                serviceHelper.Logger.Log("Sensus service received start command (startId=" + startId + ", flags=" + flags + ").", LoggingLevel.Normal, GetType());

                // promote this service to a foreground, for several reasons:  it's honest and transparent. it lets us work effectively with the 
                // android 8.0 restrictions on background services (we can run forever without being killed, we receive background location 
                // updates). it's okay to call this multiple times. doing so will simply update the notification.
                if (_foregroundServiceNotificationBuilder == null)
                {
                    PendingIntent mainActivityPendingIntent = PendingIntent.GetActivity(this, 0, new Intent(this, typeof(AndroidMainActivity)), 0);
                    _foregroundServiceNotificationBuilder = (SensusContext.Current.Notifier as AndroidNotifier).CreateNotificationBuilder(this, AndroidNotifier.SensusNotificationChannel.ForegroundService)
                                                                                                               .SetSmallIcon(Resource.Drawable.ic_launcher)
                                                                                                               .SetContentIntent(mainActivityPendingIntent)
                                                                                                               .SetOngoing(true);
                    UpdateForegroundServiceNotificationBuilder();
                    StartForeground(FOREGROUND_SERVICE_NOTIFICATION_ID, _foregroundServiceNotificationBuilder.Build());
                }
                else
                {
                    ReissueForegroundServiceNotification();
                }

                // acquire wake lock before this method returns to ensure that the device does not sleep prematurely, interrupting the execution of a callback.
                serviceHelper.KeepDeviceAwake();

                Task.Run(async () =>
                {
                    // the service can be stopped without destroying the service object. in such cases, 
                    // subsequent calls to start the service will not call OnCreate. therefore, it's 
                    // important that any code called here is okay to call multiple times, even if the 
                    // service is running. calling this when the service is running can happen because 
                    // sensus receives a signal on device boot and for any callback alarms that are 
                    // requested. furthermore, all calls here should be nonblocking / async so we don't 
                    // tie up the UI thread.
                    await serviceHelper.StartAsync();

                    if (intent == null)
                    {
                        serviceHelper.LetDeviceSleep();
                    }
                    else
                    {
                        DisplayPage displayPage;

                        // is this a callback intent?
                        if (intent.GetBooleanExtra(CallbackScheduler.SENSUS_CALLBACK_KEY, false))
                        {
                            // service the callback -- the matching LetDeviceSleep will be called therein
                            await (SensusContext.Current.CallbackScheduler as AndroidCallbackScheduler).ServiceCallbackAsync(intent);
                        }
                        // should we display a page?
                        else if (Enum.TryParse(intent.GetStringExtra(Notifier.DISPLAY_PAGE_KEY), out displayPage))
                        {
                            serviceHelper.BringToForeground();
                            SensusContext.Current.Notifier.OpenDisplayPage(displayPage);
                            serviceHelper.LetDeviceSleep();
                        }
                        else
                        {
                            serviceHelper.LetDeviceSleep();
                        }
                    }
                });
            }

            // if the service is killed by the system (e.g., due to resource constraints), ask the system to restart
            // the service when possible.
            return StartCommandResult.Sticky;
        }

        private void UpdateForegroundServiceNotificationBuilder()
        {
            AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

            int numRunningStudies = serviceHelper.RunningProtocolIds.Count;
            _foregroundServiceNotificationBuilder.SetContentTitle("You are enrolled in " + numRunningStudies + " " + (numRunningStudies == 1 ? "study" : "studies") + ".");

            string contentText = "";

            // although the number of studies might be greater than 0, the protocols might not yet be started (e.g., after starting sensus).
            List<Protocol> runningProtocols = serviceHelper.GetRunningProtocols();
            if (runningProtocols.Count > 0)
            {
                double avgParticipation = runningProtocols.Average(protocol => protocol.Participation) * 100;
                contentText += "Your overall participation level is " + Math.Round(avgParticipation, 0) + "%. ";
            }

            contentText += "Tap to open Sensus.";

            _foregroundServiceNotificationBuilder.SetContentText(contentText);
        }

        public void ReissueForegroundServiceNotification()
        {
            UpdateForegroundServiceNotificationBuilder();
            (GetSystemService(NotificationService) as NotificationManager).Notify(FOREGROUND_SERVICE_NOTIFICATION_ID, _foregroundServiceNotificationBuilder.Build());
        }

        public override IBinder OnBind(Intent intent)
        {
            AndroidSensusServiceBinder binder = new AndroidSensusServiceBinder(SensusServiceHelper.Get() as AndroidSensusServiceHelper);

            lock (_bindings)
            {
                _bindings.Add(binder);
            }

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
                {
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
                }

                _bindings.Clear();
            }
        }

        public override void OnDestroy()
        {
            Console.Error.WriteLine("--------------------------- Destroying Service ---------------------------");

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

            // do this last so that we don't dispose the service and its system services too early.
            base.OnDestroy();
        }
    }
}