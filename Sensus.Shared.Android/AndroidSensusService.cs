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
using Android.Provider;
using Sensus.Shared;
using Sensus.Shared.Context;
using Sensus.Shared.Exceptions;
using Sensus.Shared.Android.Context;
using Sensus.Shared.Android.Exceptions;
using Sensus.Shared.Android.Callbacks;
using Sensus.Shared.Callbacks;
using Xamarin.Forms.Platform.Android;

namespace Sensus.Shared.Android
{
    /// <summary>
    /// Android sensus service. Manages background running of Sensus. This is a hybrid service (http://developer.android.com/guide/components/services.html), in that
    /// it is started by the Sensus activity to run indefinitely, but the activity also binds to it to manage the Sensus system (e.g., creating protocols, starting
    /// and stopping them, etc. For now, nobody other than the Sensus activity can interact with the service (Exported = false). Perhaps we'll allow this in the future
    /// to support integration with other apps.
    /// </summary>
    [Service(Exported = false)]
    public class AndroidSensusService<MainActivityT> : Service where MainActivityT : FormsApplicationActivity
    {
        private readonly List<AndroidSensusServiceBinder<MainActivityT>> _bindings = new List<AndroidSensusServiceBinder<MainActivityT>>();

        public override void OnCreate()
        {
            base.OnCreate();

            InsightsInitialization.Initialize(new AndroidInsightsInitializer(Settings.Secure.GetString(ContentResolver, Settings.Secure.AndroidId)), SensusServiceHelper.XAMARIN_INSIGHTS_APP_KEY);

            SensusContext.Current = new AndroidSensusContext<MainActivityT>(SensusServiceHelper.ENCRYPTION_KEY, this);
            SensusServiceHelper.Initialize(() => new AndroidSensusServiceHelper<MainActivityT>());

            AndroidSensusServiceHelper<MainActivityT> serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper<MainActivityT>;

            // we might have failed to create the service helper. it's also happened that the service is created after the 
            // service helper is disposed:  https://insights.xamarin.com/app/Sensus-Production/issues/46
            if (serviceHelper == null)
                Stop();
            else
                serviceHelper.Service = this;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            AndroidSensusServiceHelper<MainActivityT> serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper<MainActivityT>;

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
                    if (intent == null)
                        serviceHelper.LetDeviceSleep();
                    else
                    {
                        DisplayPage displayPage;

                        // is this a callback intent?
                        if (intent.GetBooleanExtra(CallbackScheduler.SENSUS_CALLBACK_KEY, false))
                            (SensusContext.Current.CallbackScheduler as AndroidCallbackScheduler<MainActivityT>).ServiceCallback(intent);
                        // should we display a page
                        else if (Enum.TryParse(intent.GetStringExtra(Notifier.DISPLAY_PAGE_KEY), out displayPage))
                        {
                            serviceHelper.BringToForeground();
                            SensusContext.Current.Notifier.OpenDisplayPage(displayPage);
                            serviceHelper.LetDeviceSleep();
                        }
                        else
                            serviceHelper.LetDeviceSleep();
                    }
                });
            }

            return StartCommandResult.Sticky;
        }

        public override IBinder OnBind(Intent intent)
        {
            AndroidSensusServiceBinder<MainActivityT> binder = new AndroidSensusServiceBinder<MainActivityT>(SensusServiceHelper.Get() as AndroidSensusServiceHelper<MainActivityT>);

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
                foreach (AndroidSensusServiceBinder<MainActivityT> binder in _bindings)
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

            AndroidSensusServiceHelper<MainActivityT> serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper<MainActivityT>;

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
                serviceHelper.Service = null;
            }
        }
    }
}