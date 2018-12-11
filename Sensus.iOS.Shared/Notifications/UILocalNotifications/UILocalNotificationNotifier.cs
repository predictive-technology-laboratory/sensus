//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using Foundation;
using Sensus.Notifications;
using Sensus.Context;
using UIKit;
using Xamarin.Forms.Platform.iOS;
using System.Threading.Tasks;

namespace Sensus.iOS.Notifications.UILocalNotifications
{
    public class UILocalNotificationNotifier : iOSNotifier
    {
        private Dictionary<string, UILocalNotification> _idNotification;

        public UILocalNotificationNotifier()
        {
            _idNotification = new Dictionary<string, UILocalNotification>();
        }

        public override Task IssueNotificationAsync(string title, string message, string id, Protocol protocol, bool alertUser, DisplayPage displayPage)
        {
            IssueNotification(title, message, id, protocol, alertUser, displayPage, DateTime.Now, null);
            return Task.CompletedTask;
        }

        public void IssueSilentNotification(string id, DateTime fireDateTime, NSMutableDictionary notificationInfo, Action<UILocalNotification> notificationCreated = null)
        {
            if (notificationInfo == null)
            {
                notificationInfo = new NSMutableDictionary();
            }

            // the user should never see a silent notification since we cancel them when the app is backgrounded. but there are race conditions that
            // might result in a silent notifiation being scheduled just before the app is backgrounded. give a generic message so that the notification
            // isn't totally confusing to the user.
            IssueNotification("Please open this notification.", "One of your studies needs to be updated.", id, null, false, DisplayPage.None, fireDateTime, notificationInfo, notificationCreated);
        }

        public void IssueNotification(string title, string message, string id, Protocol protocol, bool alertUser, DisplayPage displayPage, DateTime fireDateTime, NSMutableDictionary notificationInfo, Action<UILocalNotification> notificationCreated = null)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                CancelNotification(id);

                if (notificationInfo == null)
                {
                    notificationInfo = new NSMutableDictionary();
                }

                notificationInfo.SetValueForKey(new NSString(id), new NSString(NOTIFICATION_ID_KEY));
                notificationInfo.SetValueForKey(new NSString(displayPage.ToString()), new NSString(DISPLAY_PAGE_KEY));

                // all properties below were introduced in iOS 8.0. we currently target 9.0 and above, so these should be safe to set.
                UILocalNotification notification = new UILocalNotification
                {
                    AlertBody = message,
                    TimeZone = null,  // null for UTC interpretation of FireDate
                    FireDate = fireDateTime.ToUniversalTime().ToNSDate(),
                    UserInfo = notificationInfo
                };

                // introduced in 8.0...protocol might be null when issuing the pending surveys notification.
                if (alertUser && (protocol == null || !protocol.TimeIsWithinAlertExclusionWindow(fireDateTime.TimeOfDay)))
                {
                    notification.SoundName = UILocalNotification.DefaultSoundName;
                }

                // introduced in iOS 8.2:  https://developer.apple.com/reference/uikit/uilocalnotification/1616647-alerttitle
                if (UIDevice.CurrentDevice.CheckSystemVersion(8, 2) && !string.IsNullOrWhiteSpace(title))
                {
                    notification.AlertTitle = title;
                }

                notificationCreated?.Invoke(notification);

                IssueNotification(notification);
            });
        }

        public void IssueNotification(UILocalNotification notification)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                string id = null;

                lock (_idNotification)
                {
                    // notifications are not required to have an id. if the current one does, save the notification by id for easy lookup later.
                    // use indexing to add/replace since we might be reissuing the notification with the same id.
                    id = notification.UserInfo?.ValueForKey(new NSString(NOTIFICATION_ID_KEY))?.ToString();
                    if (id != null)
                    {
                        _idNotification[id] = notification;
                    }
                }

                UIApplication.SharedApplication.ScheduleLocalNotification(notification);

                SensusServiceHelper.Get().Logger.Log("Notification " + (id ?? "[null]") + " requested for " + notification.FireDate + ".", LoggingLevel.Normal, GetType());
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
            {
                return;
            }

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
                    {
                        UIApplication.SharedApplication.CancelLocalNotification(scheduledNotification);
                    }
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
    }
}
