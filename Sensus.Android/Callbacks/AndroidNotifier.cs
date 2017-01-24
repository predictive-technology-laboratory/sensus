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

using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Media;
using Sensus.Callbacks;

namespace Sensus.Android.Callbacks
{
    public class AndroidNotifier : Notifier
    {
        private AndroidSensusService _service;
        private NotificationManager _notificationManager;

        public AndroidNotifier(AndroidSensusService service)
        {
            _service = service;
            _notificationManager = _service.GetSystemService(global::Android.Content.Context.NotificationService) as NotificationManager;
        }

        public override void IssueNotificationAsync(string title, string message, string id, string protocolId, bool alertUser, DisplayPage displayPage)
        {
            if (_notificationManager == null)
                return;

            Task.Run(() =>
            {
                if (message == null)
                    CancelNotification(id);
                else
                {
                    Intent notificationIntent = new Intent(_service, typeof(AndroidSensusService));
                    notificationIntent.PutExtra(DISPLAY_PAGE_KEY, displayPage.ToString());

                    PendingIntent notificationPendingIntent = PendingIntent.GetService(_service, 0, notificationIntent, PendingIntentFlags.UpdateCurrent);

                    Notification.Builder notificationBuilder = new Notification.Builder(_service)
                        .SetContentTitle(title)
                        .SetContentText(message)
                        .SetSmallIcon(Resource.Drawable.ic_launcher)
                        .SetContentIntent(notificationPendingIntent)
                        .SetAutoCancel(true)
                        .SetOngoing(false);

                    // only check the protocol's Notification Alert Exclusion Windows to determine whether to cancel vibration and sound
                    // if the alertUser parameter is true and a protocol ID has been passed to the method.
                    if (alertUser && protocolId != null)
                    {
                        alertUser = !NotificationTimeIsWithinAlertExclusionWindow(protocolId, DateTime.Now);
                    }

                    if (alertUser)
                    {
                        notificationBuilder.SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification));
                        notificationBuilder.SetVibrate(new long[] { 0, 250, 50, 250 });
                    }

                    _notificationManager.Notify(id, 0, notificationBuilder.Build());
                }
            });
        }

        public override void CancelNotification(string id)
        {
            _notificationManager.Cancel(id, 0);
        }
    }
}