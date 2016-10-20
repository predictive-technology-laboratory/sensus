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
using Sensus.Shared.Callbacks;

namespace Sensus.Shared.Android.Callbacks
{
    public class AndroidNotifier: Notifier
    {
        #region Fields
        private readonly Service _androidService;
        private readonly int _smallIcon;
        private readonly NotificationManager _notificationManager;
        #endregion

        #region Constructors
        public AndroidNotifier(Service androidService, int smallIcon)
        {
            _androidService      = androidService;
            _smallIcon           = smallIcon;
            _notificationManager = (NotificationManager)androidService.GetSystemService(global::Android.Content.Context.NotificationService);
        }
        #endregion

        #region Public Methods
        public override void IssueNotificationAsync(string message, string id, bool playSound, bool vibrate)
        {
            IssueNotificationAsync("Sensus", message, true, false, id, playSound, vibrate);
        }
        #endregion

        #region Private Methods
        public void IssueNotificationAsync(string title, string message, bool autoCancel, bool ongoing, string tag, bool playSound, bool vibrate)
        {
            if (_notificationManager == null) return;

            Task.Run(() =>
            {
                if (message == null)
                {
                    _notificationManager.Cancel(tag, 0);
                    return;
                }

                var intent  = new Intent(_androidService, _androidService.GetType());
                var pending = PendingIntent.GetService(_androidService, 0, intent, PendingIntentFlags.UpdateCurrent);

                var builder = new Notification.Builder(_androidService).SetContentTitle(title).SetContentText(message).SetSmallIcon(_smallIcon).SetContentIntent(pending).SetAutoCancel(autoCancel).SetOngoing(ongoing);

                if (playSound)
                {
                    builder.SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification));
                }

                if (vibrate)
                {
                    builder.SetVibrate(new long[] { 0, 250, 50, 250 });
                }

                _notificationManager.Notify(tag, 0, builder.Build());
            });
        }
        #endregion  
    }
}
