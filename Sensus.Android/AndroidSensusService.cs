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
using Android.OS;
using Android.Widget;
using SensusService;

namespace Sensus.Android
{
    [Service]
    public class AndroidSensusService : Service
    {
        private NotificationManager _notificationManager;
        private Notification.Builder _notificationBuilder;
        private AndroidSensusServiceHelper _sensusServiceHelper;
        private const int ServiceNotificationId = 0;
        private const int NotificationPendingIntentId = 1;

        public override void OnCreate()
        {
            base.OnCreate();

            _notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
            _notificationBuilder = new Notification.Builder(this);

            _sensusServiceHelper = new AndroidSensusServiceHelper();
            _sensusServiceHelper.Stopped += (o, e) =>
                {
                    _notificationManager.Cancel(ServiceNotificationId);
                    StopSelf();
                };
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            _sensusServiceHelper.Logger.Log("Sensus service received start command (startId=" + startId + ").", LoggingLevel.Normal);

            // the service can be stopped without destroying the service object. in such cases, 
            // subsequent calls to start the service will not call OnCreate, which is why the 
            // following code needs to run here -- e.g., starting the helper object and displaying
            // the notification. therefore, it's important that any code called here is
            // okay to call multiple times, even if the service is running. calling this when
            // the service is running can happen because sensus receives a signal on device
            // boot to start the service, and then when the sensus app is started the service
            // start method is called again.

            _sensusServiceHelper.Start();

            TaskStackBuilder stackBuilder = TaskStackBuilder.Create(this);
            stackBuilder.AddParentStack(Java.Lang.Class.FromType(typeof(MainActivity)));
            stackBuilder.AddNextIntent(new Intent(this, typeof(MainActivity)));

            PendingIntent pendingIntent = stackBuilder.GetPendingIntent(NotificationPendingIntentId, PendingIntentFlags.OneShot);

            _notificationBuilder.SetContentTitle("Sensus")
                                .SetContentText("Tap to Open")
                                .SetSmallIcon(Resource.Drawable.ic_launcher)
                                .SetContentIntent(pendingIntent)
                                .SetAutoCancel(true)
                                .SetOngoing(true);

            _notificationManager.Notify(ServiceNotificationId, _notificationBuilder.Build());

            if (intent.GetBooleanExtra(AndroidSensusServiceHelper.INTENT_EXTRA_NAME_PING, false))
                _sensusServiceHelper.Ping();

            return StartCommandResult.RedeliverIntent;
        }

        public override void OnDestroy()
        {
            _notificationManager.Cancel(ServiceNotificationId);

            _sensusServiceHelper.Destroy();

            base.OnDestroy();
        }

        public override IBinder OnBind(Intent intent)
        {
            return new AndroidSensusServiceBinder(_sensusServiceHelper);
        }
    }
}