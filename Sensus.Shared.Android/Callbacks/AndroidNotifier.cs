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

using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Media;
using Sensus.Android;
using Sensus.Shared.Callbacks;
using Xamarin.Forms.Platform.Android;

namespace Sensus.Shared.Android.Callbacks
{
    public class AndroidNotifier<MainActivityT> : Notifier where MainActivityT : FormsApplicationActivity
    {
        private AndroidSensusService<MainActivityT> _service;
        private NotificationManager _notificationManager;

        public AndroidNotifier(AndroidSensusService<MainActivityT> service)
        {
            _service = service;
            _notificationManager = _service.GetSystemService(global::Android.Content.Context.NotificationService) as NotificationManager;
        }

        public override void IssueNotificationAsync(string title, string message, string id, bool playSound, DisplayPage displayPage)
        {
            if (_notificationManager != null)
            {
                Task.Run(() =>
                {
                    if (message == null)
                        _notificationManager.Cancel(id, 0);
                    else
                    {
                        Intent serviceIntent = new Intent(_service, typeof(AndroidSensusService<MainActivityT>));
                        PendingIntent pendingIntent = PendingIntent.GetService(_service, 0, serviceIntent, PendingIntentFlags.UpdateCurrent);

                        Notification.Builder builder = new Notification.Builder(_service)
                            .SetContentTitle(title)
                            .SetContentText(message)
                            .SetContentIntent(pendingIntent)
                            .SetAutoCancel(true)
                            .SetOngoing(false);

                        if (playSound)
                        {
                            builder.SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification));
                            builder.SetVibrate(new long[] { 0, 250, 50, 250 });
                        }

                        _notificationManager.Notify(id, 0, builder.Build());
                    }
                });
            }
        }

        public override void CancelNotification(string id)
        {
            _notificationManager.Cancel(id, 0);
        }
    }
}
