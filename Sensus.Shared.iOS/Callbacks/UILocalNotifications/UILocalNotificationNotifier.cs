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
using System.Collections.Generic;
using Foundation;
using Sensus.Callbacks;
using Sensus.Context;
using UIKit;
using Xamarin.Forms.Platform.iOS;

namespace Sensus.iOS.Callbacks.UILocalNotifications
{
    public class UILocalNotificationNotifier : iOSNotifier, IUILocalNotificationNotifier
    {
        private Dictionary<string, UILocalNotification> _idNotification;

        public UILocalNotificationNotifier()
        {
            _idNotification = new Dictionary<string, UILocalNotification>();
        }

        public override void IssueNotificationAsync(string title, string message, string id, bool playSound, DisplayPage displayPage)
        {
            IssueNotificationAsync(title, message, id, playSound, displayPage, 0, null);
        }

        public void IssueSilentNotificationAsync(string id, int delayMS, NSMutableDictionary notificationInfo, Action<UILocalNotification> notificationCreated = null)
        {
            if (notificationInfo == null)
                notificationInfo = new NSMutableDictionary();

            notificationInfo.SetValueForKey(new NSNumber(true), new NSString(SILENT_NOTIFICATION_KEY));

            // the user should never see a silent notification since we cancel them when the app is backgrounded. but there are race conditions that
            // might result in a silent notifiation being scheduled just before the app is backgrounded. give a generic message so that the notification
            // isn't totally confusing to the user.
            IssueNotificationAsync("Please open this notification.", "One of your studies needs to be updated.", id, false, DisplayPage.None, delayMS, notificationInfo, notificationCreated);
        }

        public void IssueNotificationAsync(string title, string message, string id, bool playSound, DisplayPage displayPage, int delayMS, NSMutableDictionary notificationInfo, Action<UILocalNotification> notificationCreated = null)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                CancelNotification(id);

                if (notificationInfo == null)
                    notificationInfo = new NSMutableDictionary();

                notificationInfo.SetValueForKey(new NSString(id), new NSString(NOTIFICATION_ID_KEY));
                notificationInfo.SetValueForKey(new NSString(displayPage.ToString()), new NSString(DISPLAY_PAGE_KEY));

                // all properties below were introduced in iOS 8.0. we currently target 8.0 and above, so these should be safe to set.
                UILocalNotification notification = new UILocalNotification
                {
                    AlertBody = message,
                    TimeZone = null,  // null for UTC interpretation of FireDate
                    FireDate = DateTime.UtcNow.ToNSDate().AddSeconds(delayMS / 1000d),
                    UserInfo = notificationInfo
                };

                // also introduced in 8.0
                if (playSound)
                    notification.SoundName = UILocalNotification.DefaultSoundName;

                // introduced in iOS 8.2:  https://developer.apple.com/reference/uikit/uilocalnotification/1616647-alerttitle
                if (UIDevice.CurrentDevice.CheckSystemVersion(8, 2) && !string.IsNullOrWhiteSpace(title))
                    notification.AlertTitle = title;

                notificationCreated?.Invoke(notification);

                IssueNotificationAsync(notification);
            });
        }

        public void IssueNotificationAsync(UILocalNotification notification)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                lock (_idNotification)
                {
                    // notifications are not required to have an id. if the current one does, save the notification by id for easy lookup later.
                    // use indexing to add/replace since we might be reissuing the notification with the same id.
                    string id = notification.UserInfo?.ValueForKey(new NSString(NOTIFICATION_ID_KEY))?.ToString();
                    if (id != null)
                        _idNotification[id] = notification;
                }

                UIApplication.SharedApplication.ScheduleLocalNotification(notification);
            });
        }

        public void CancelNotification(UILocalNotification notification)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                UIApplication.SharedApplication.CancelLocalNotification(notification);
                CancelNotification(notification.UserInfo?.ValueForKey(new NSString(NOTIFICATION_ID_KEY))?.ToString());
            });
        }

        public override void CancelNotification(string id)
        {
            if (id == null)
                return;

            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                // a local notification can be scheduled, in which case it hasn't yet been delivered and should reside within the shared 
                // application's list of scheduled notifications. the tricky part here is that these notification objects aren't reference-equal, 
                // so we can't just pass `notification` to CancelLocalNotification. instead, we must search for the notification by id and 
                // cancel the appropriate scheduled notification object.
                foreach (UILocalNotification scheduledNotification in UIApplication.SharedApplication.ScheduledLocalNotifications)
                {
                    string scheduledNotificationId = scheduledNotification.UserInfo?.ValueForKey(new NSString(NOTIFICATION_ID_KEY))?.ToString();
                    if (scheduledNotificationId == id)
                        UIApplication.SharedApplication.CancelLocalNotification(scheduledNotification);
                }

                // cancel by object, too, in case the notification has been delivered and is no longer scheduled.
                lock (_idNotification)
                {
                    UILocalNotification notification;
                    if (_idNotification.TryGetValue(id, out notification))
                    {
                        UIApplication.SharedApplication.CancelLocalNotification(notification);
                        _idNotification.Remove(id);
                    }
                }
            });
        }

        public override void CancelSilentNotifications()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                foreach (UILocalNotification scheduledNotification in UIApplication.SharedApplication.ScheduledLocalNotifications)
                {
                    if (IsSilent(scheduledNotification.UserInfo))
                    {
                        CancelNotification(scheduledNotification);
                    }
                }
            });
        }
    }
}