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
using Sensus.Android.Callbacks;
using Sensus.Android.Concurrent;
using Sensus.Encryption;
using System.Threading.Tasks;
using Sensus.Android.Notifications;
using Plugin.CurrentActivity;

// the unit test project contains the Resource class in its namespace rather than the Sensus.Android
// namespace. include that namespace below.
#if UNIT_TEST
using Sensus.Android.Tests;
#endif

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
        public const string STOP_SERVICE_IF_NO_PROTOCOLS_SHOULD_RUN = "STOP-SERVICE-IF-NO-PROTOCOLS-SHOULD-RUN";

        public static Intent GetIntent() => new Intent(Application.Context, typeof(AndroidSensusService));

        /// <summary>
        /// Starts the service.
        /// </summary>
        /// <returns>The service.</returns>
        /// <param name="context">Context.</param>
        /// <param name="stopServiceIfNoProtocolsShouldRun">If set to <c>true</c> stop service if no protocols should run.</param>
        public static Intent Start(global::Android.Content.Context context, bool stopServiceIfNoProtocolsShouldRun)
        {
            Intent serviceIntent = GetIntent();
            serviceIntent.PutExtra(STOP_SERVICE_IF_NO_PROTOCOLS_SHOULD_RUN, stopServiceIfNoProtocolsShouldRun);

            // after android 26, starting a foreground service requires the use of StartForegroundService rather than StartService.
            // in either case, the service itself will call StartForeground after it has started. more info:  
            // 
            //     https://developer.android.com/reference/android/content/Context.html#startForegroundService(android.content.Intent)
            //
            // also see notes on backwards compatibility for how the compiler directives are used below:
            //
            //    see the Backwards Compatibility article for more information
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

        private readonly List<AndroidSensusServiceBinder> _bindings = new List<AndroidSensusServiceBinder>();
        private AndroidPowerConnectionChangeBroadcastReceiver _powerBroadcastReceiver;

        public override void OnCreate()
        {
            base.OnCreate();

            // initialize the current activity plugin here as well as in the main activity
            // since this service may be created by iteself without a main activity (e.g., 
            // on boot or on OS restart of the service). we want the plugin to have be 
            // initialized regardless of how the app comes to be created.
            CrossCurrentActivity.Current.Init(Application);

            SensusContext.Current = new AndroidSensusContext
            {
                Platform = Platform.Android,
                MainThreadSynchronizer = new MainConcurrent(),
                SymmetricEncryption = new SymmetricEncryption(SensusServiceHelper.ENCRYPTION_KEY),
                CallbackScheduler = new AndroidCallbackScheduler(this),
                Notifier = new AndroidNotifier(),
                PowerConnectionChangeListener = new AndroidPowerConnectionChangeListener()
            };

            // promote this service to a foreground service as soon as possible. we use a foreground service for several 
            // reasons. it's honest and transparent. it lets us work effectively with the android 8.0 restrictions on 
            // background services. we can run forever without being killed. we receive background location updates, etc.
            (SensusContext.Current.Notifier as AndroidNotifier).UpdateForegroundServiceNotificationBuilder();
            StartForeground(AndroidNotifier.FOREGROUND_SERVICE_NOTIFICATION_ID, (SensusContext.Current.Notifier as AndroidNotifier).BuildForegroundServiceNotification());

            // https://developer.android.com/reference/android/content/Intent#ACTION_POWER_CONNECTED
            // This is intended for applications that wish to register specifically to this notification. Unlike ACTION_BATTERY_CHANGED, 
            // applications will be woken for this and so do not have to stay active to receive this notification. This action can be 
            // used to implement actions that wait until power is available to trigger.
            // 
            // We use the same receiver for both the connected and disconnected intents.
            _powerBroadcastReceiver = new AndroidPowerConnectionChangeBroadcastReceiver();
            IntentFilter powerConnectFilter = new IntentFilter();
            powerConnectFilter.AddAction(Intent.ActionPowerConnected);
            powerConnectFilter.AddAction(Intent.ActionPowerDisconnected);
            powerConnectFilter.AddCategory(Intent.CategoryDefault);
            RegisterReceiver(_powerBroadcastReceiver, powerConnectFilter);

            // must come after context initialization. also, it is here -- below StartForeground -- because it can
            // take a while to complete and we don't want to run afoul of the short timing requirements on calling
            // StartForeground.
            SensusServiceHelper.Initialize(() => new AndroidSensusServiceHelper());

            AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

            // we might have failed to create the service helper. it's also happened that the service is created 
            // after the service helper is disposed.
            if (serviceHelper == null)
            {
                Stop();
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

                // update the foreground service notification with information about loaded/running studies.
                (SensusContext.Current.Notifier as AndroidNotifier).ReissueForegroundServiceNotification();

                // if the service started but there are no protocols that should be running, then stop the app now. there is no 
                // reason for the app to be running in this situation, and the user will likely be annoyed at the presence of the 
                // foreground service notification.
                if (intent != null && intent.GetBooleanExtra(STOP_SERVICE_IF_NO_PROTOCOLS_SHOULD_RUN, false) && serviceHelper.RunningProtocolIds.Count == 0)
                {
                    serviceHelper.Logger.Log("Started service without running protocols. Stopping service now.", LoggingLevel.Normal, GetType());
                    Stop();
                    return StartCommandResult.NotSticky;
                }

                // acquire wake lock before this method returns to ensure that the device does not sleep prematurely, interrupting the execution of a callback.
                serviceHelper.KeepDeviceAwake();

                Task.Run(async () =>
                {
                    try
                    {
                        // the service can be stopped without destroying the service object. in such cases, 
                        // subsequent calls to start the service will not call OnCreate. therefore, it's 
                        // important that any code called here is okay to call multiple times, even if the 
                        // service is running. calling this when the service is running can happen because 
                        // sensus receives a signal on device boot and for any callback alarms that are 
                        // requested. furthermore, all calls here should be nonblocking / async so we don't 
                        // tie up the UI thread.
                        await serviceHelper.StartAsync();

                        if (intent != null)
                        {
                            AndroidCallbackScheduler callbackScheduler = SensusContext.Current.CallbackScheduler as AndroidCallbackScheduler;

                            if (callbackScheduler.IsCallback(intent))
                            {
                                await callbackScheduler.ServiceCallbackAsync(intent);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        serviceHelper.Logger.Log("Exception while responding to on-start command:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                    finally
                    {
                        serviceHelper.LetDeviceSleep();
                    }
                });
            }

            // if the service is killed by the system (e.g., due to resource constraints), ask the system to restart
            // the service when possible.
            return StartCommandResult.Sticky;
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
                foreach (AndroidSensusServiceBinder binding in _bindings)
                {
                    try
                    {
                        binding.ServiceStopAction?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while notifying binding of service stop:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                }

                _bindings.Clear();
            }
        }

        public override void OnDestroy()
        {
            Console.Error.WriteLine("--------------------------- Destroying Service ---------------------------");

            // we used to stop all protocols when destroying the service, but this is not appropriate. if we're
            // destroying the service because the user has stopped the app, then we'll already have stopped the
            // protocols as requested by the user. if the os is destroying the service to reclaim resources, then
            // the protocol should be left running. if the os subsequently kills the app's process, the protocols
            // will restart when the os resumes the process and service. so, don't stop the protocols.

            SensusServiceHelper.Get()?.Logger.Log("Destroying service.", LoggingLevel.Normal, GetType());  // the service helper will be null if we failed to create it within OnCreate

            NotifyBindingsOfStop();

            // we've seen cases where the receiver doesn't get registered before the service is 
            // destroyed. catch exception raised from attempting to unregister a receiver that 
            // hasn't been registered.
            try
            {
                UnregisterReceiver(_powerBroadcastReceiver);
            }
            catch (Exception)
            { }

            try
            {
                (SensusContext.Current.Notifier as AndroidNotifier).OnDestroy();
            }
            catch(Exception)
            { }
            
            // do this last so that we don't dispose the service and its system services too early.
            base.OnDestroy();
        }
    }
}