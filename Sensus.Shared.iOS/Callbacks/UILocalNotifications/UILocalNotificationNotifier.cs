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
using Foundation;
using Sensus.Shared.Callbacks;
using Sensus.Shared.Context;
using UIKit;
using Xamarin.Forms.Platform.iOS;

namespace Sensus.Shared.iOS.Callbacks.UILocalNotifications
{
    public class UILocalNotificationNotifier : iOSNotifier, IUILocalNotificationNotifier
    {
        public override void IssueNotificationAsync(string title, string message, string id, bool playSound, DisplayPage displayPage)
        {
            IssueNotificationAsync(title, message, id, playSound, displayPage, 0, null);
        }

        public void IssueSilentNotificationAsync(string id, int delayMS, NSMutableDictionary notificationInfo)
        {
            if (notificationInfo == null)
                notificationInfo = new NSMutableDictionary();

            notificationInfo.SetValueForKey(new NSNumber(true), new NSString(SILENT_NOTIFICATION_KEY));

            IssueNotificationAsync("silent", "silent", id, false, DisplayPage.None, delayMS, notificationInfo);
        }

        public void IssueNotificationAsync(string title, string message, string id, bool playSound, DisplayPage displayPage, int delayMS, NSMutableDictionary notificationInfo)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
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

                // also in 8.0
                if (playSound)
                    notification.SoundName = UILocalNotification.DefaultSoundName;

                // introduced in iOS 8.2:  https://developer.apple.com/reference/uikit/uilocalnotification/1616647-alerttitle
                if (UIDevice.CurrentDevice.CheckSystemVersion(8, 2) && !string.IsNullOrWhiteSpace(title))
                    notification.AlertTitle = title;

                UIApplication.SharedApplication.ScheduleLocalNotification(notification);
            });
        }

        public void CancelNotification(UILocalNotification notification)
        {
            // set up action to cancel notification
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                UIApplication.SharedApplication.CancelLocalNotification(notification);
                CancelNotification(notification.UserInfo?.ValueForKey(new NSString(NOTIFICATION_ID_KEY))?.ToString());
            });
        }

        public override void CancelNotification(string id)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                // a local notification can be scheduled, in which case it hasn't yet been delivered and should reside within the shared 
                // application's list of scheduled notifications. the tricky part here is that these notification objects aren't reference-equal, 
                // so we can't just pass `notification` to CancelLocalNotification. instead, we must search for the notification by id and 
                // cancel the appropriate scheduled notification object.
                foreach (UILocalNotification scheduledNotification in UIApplication.SharedApplication.ScheduledLocalNotifications)
                {
                    string scheduledId = scheduledNotification.UserInfo?.ValueForKey(new NSString(NOTIFICATION_ID_KEY))?.ToString();

                    if (scheduledId == id)
                    {
                        UIApplication.SharedApplication.CancelLocalNotification(scheduledNotification);
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
                    if ((scheduledNotification.UserInfo?.ValueForKey(new NSString(SILENT_NOTIFICATION_KEY)) as NSNumber)?.BoolValue ?? false)
                    {
                        CancelNotification(scheduledNotification);
                    }
                }
            });
        }
    }
}