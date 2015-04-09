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

namespace Sensus.Android
{
    [Service]
    public class AndroidSensusService : Service
    {
        private const int SERVICE_NOTIFICATION_ID = 0;
        private const int NOTIFICATION_PENDING_INTENT_ID = 1;

        private NotificationManager _notificationManager;
        private Notification _notification;
        private AndroidSensusServiceHelper _sensusServiceHelper;

        public override void OnCreate()
        {
            base.OnCreate();

            _notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;

            UpdateNotification("Sensus", "");

            _sensusServiceHelper = SensusServiceHelper.Load<AndroidSensusServiceHelper>() as AndroidSensusServiceHelper;
            _sensusServiceHelper.SetService(this);
            _sensusServiceHelper.Stopped += (o, e) =>
                {
                    _notificationManager.Cancel(SERVICE_NOTIFICATION_ID);
                    StopSelf();
                };
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            _sensusServiceHelper.Logger.Log("Sensus service received start command (startId=" + startId + ").", LoggingLevel.Debug, GetType());

            // the service can be stopped without destroying the service object. in such cases, 
            // subsequent calls to start the service will not call OnCreate, which is why the 
            // following code needs to run here -- e.g., starting the helper object and displaying
            // the notification. therefore, it's important that any code called here is
            // okay to call multiple times, even if the service is running. calling this when
            // the service is running can happen because sensus receives a signal on device
            // boot and for any callback alarms that are requested. furthermore, all calls here
            // should be nonblocking / async so we don't tie up the UI thread.

            _sensusServiceHelper.StartAsync(() =>
                {
                    if (intent.GetBooleanExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_KEY, false))
                    {
                        int callbackId = intent.GetIntExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_ID_KEY, -1);
                        if (callbackId >= 0)
                        {
                            bool repeating = intent.GetBooleanExtra(AndroidSensusServiceHelper.SENSUS_CALLBACK_REPEATING_KEY, false);
                            _sensusServiceHelper.RaiseCallbackAsync(callbackId, repeating);
                        }
                    }
                });

            return StartCommandResult.RedeliverIntent;
        }

        public override void OnDestroy()
        {
            _notificationManager.Cancel(SERVICE_NOTIFICATION_ID);

            _sensusServiceHelper.Destroy();

            base.OnDestroy();
        }

        public override IBinder OnBind(Intent intent)
        {
            return new AndroidSensusServiceBinder(_sensusServiceHelper);
        }

        public void UpdateNotification(string title, string text)
        {
            TaskStackBuilder stackBuilder = TaskStackBuilder.Create(this);
            stackBuilder.AddParentStack(Java.Lang.Class.FromType(typeof(AndroidMainActivity)));
            stackBuilder.AddNextIntent(new Intent(this, typeof(AndroidMainActivity)));
            PendingIntent pendingIntent = stackBuilder.GetPendingIntent(NOTIFICATION_PENDING_INTENT_ID, PendingIntentFlags.UpdateCurrent);

            _notification = new Notification.Builder(this).SetContentTitle(title)
                                                          .SetContentText(text)
                                                          .SetSmallIcon(Resource.Drawable.ic_launcher)
                                                          .SetContentIntent(pendingIntent)
                                                          .SetAutoCancel(false)
                                                          .SetOngoing(true).Build();

            _notificationManager.Notify(SERVICE_NOTIFICATION_ID, _notification);
        }
    }
}
