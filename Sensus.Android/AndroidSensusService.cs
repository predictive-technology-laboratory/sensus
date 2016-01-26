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
        private const int FOREGROUND_SERVICE_NOTIFICATION_ID = 1;

        private Notification _foregroundServiceNotification;

        public override void OnCreate()
        {
            base.OnCreate();

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
                serviceHelper.Logger.Log("Sensus service received start command (startId=" + startId + ").", LoggingLevel.Normal, GetType());

                if (intent == null)
                    serviceHelper.MainActivityWillBeDisplayed = false;
                else
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
                        if (intent != null && intent.GetBooleanExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_KEY, false))
                        {
                            string callbackId = intent.GetStringExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_ID_KEY);
                            if (callbackId != null)
                            {
                                bool repeating = intent.GetBooleanExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_REPEATING_KEY, false);
                                serviceHelper.RaiseCallbackAsync(callbackId, repeating, true);
                            }
                        }
                    });
            }

            if (_foregroundServiceNotification == null)
            {
                // run service as a foreground service since we want it to remain open always and not be restarted when main activity
                // is finished.
                Intent activityIntent = new Intent(this, typeof(AndroidMainActivity));
                PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, activityIntent, PendingIntentFlags.UpdateCurrent);
                _foregroundServiceNotification = new Notification.Builder(this)
                    .SetContentTitle("Sensus")
                    .SetContentText("Sensus is running. Tap to open.")
                    .SetSmallIcon(Resource.Drawable.ic_launcher)
                    .SetContentIntent(pendingIntent)
                    .SetAutoCancel(false)
                    .SetOngoing(true).Build();

                StartForeground(FOREGROUND_SERVICE_NOTIFICATION_ID, _foregroundServiceNotification);
            }

            return StartCommandResult.Sticky;
        }

        public override IBinder OnBind(Intent intent)
        {
            return new AndroidSensusServiceBinder(SensusServiceHelper.Get() as AndroidSensusServiceHelper);
        }

        public void Stop()
        {
            StopForeground(true);
            StopSelf();

            _foregroundServiceNotification = null;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            // ondestroy can be called in two different ways. first, the user might stop sensus from within the activity. if this
            // happens, the activity will be finished, the service helper will be stopped/disposed, and the service will be stopped.
            // at some indeterminate point in the future, the service will be destroyed and cleaned up, calling this method. in this 
            // case we don't need to do anything since all of the stopping/cleaning has already happened. this case is indicated
            // by a null service helper. the other way we might find ourselves in ondestroy is if system resources are running out
            // and android decides to reclaim the service. in this case we need to finish the activity if one is running and stop/dispose 
            // the service helper. this case is initiated by the system and not the user and is indicated by a non-null service helper.
           
            AndroidSensusServiceHelper serviceHelper = SensusServiceHelper.Get() as AndroidSensusServiceHelper;

            if (serviceHelper != null)
            {
                // case 2 applies (see above)...do some things.
                serviceHelper.Logger.Log("Destroying service.", LoggingLevel.Normal, GetType());
                serviceHelper.Stop(false);
                serviceHelper.SetService(null);
            }
        }
    }
}