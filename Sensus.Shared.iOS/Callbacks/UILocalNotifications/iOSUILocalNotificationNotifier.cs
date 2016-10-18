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
using System.Linq;
using Foundation;
using Sensus.Shared.Context;
using Sensus.Shared.Exceptions;
using UIKit;
using Xamarin.Forms.Platform.iOS;

namespace Sensus.Shared.iOS
{
    public class iOSUILocalNotificationNotifier : Notifier, IiOSNotifier
    {
        private List<UILocalNotification> _notifications;

        public iOSUILocalNotificationNotifier()
        {
            _notifications = new List<UILocalNotification>();
        }

        public override void IssueNotificationAsync(string message, string id, bool playSound, bool vibrate)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                // cancel any existing notifications with the given id
                lock (_notifications)
                {
                    foreach (UILocalNotification notification in _notifications.ToList())
                    {
                        string notificationId = notification.UserInfo.ValueForKey(new NSString(NOTIFICATION_ID_KEY)).ToString();
                        if (notificationId == id)
                        {
                            CancelLocalNotification(notification, NOTIFICATION_ID_KEY);
                            _notifications.Remove(notification);
                        }
                    }
                }

                // if the message is not null, then schedule the notification.
                if (message != null)
                {
                    // all properties below were introduced in iOS 8.0. we currently target 8.0 and above, so these should be safe to set.
                    UILocalNotification notification = new UILocalNotification
                    {
                        AlertBody = message,
                        TimeZone = null,  // null for UTC interpretation of FireDate
                        FireDate = DateTime.UtcNow.ToNSDate(),
                        UserInfo = new NSDictionary(NOTIFICATION_ID_KEY, id)
                    };

                    // also in 8.0
                    if (playSound)
                        notification.SoundName = UILocalNotification.DefaultSoundName;

                    // the following UILocalNotification property was introduced in iOS 8.2:  https://developer.apple.com/reference/uikit/uilocalnotification/1616647-alerttitle
                    if (UIDevice.CurrentDevice.CheckSystemVersion(8, 2))
                        notification.AlertTitle = "Sensus";

                    lock (_notifications)
                    {
                        _notifications.Add(notification);
                    }

                    UIApplication.SharedApplication.ScheduleLocalNotification(notification);
                }
            });
        }

        /// <summary>
        /// Cancels a UILocalNotification. This will succeed in one of two conditions:  (1) if the notification to be
        /// cancelled is scheduled (i.e., not delivered); or (2) if the notification to be cancelled has been delivered
        /// and if the object passed in is the delivered notification and not the one that was passed to
        /// ScheduleLocalNotification -- once passed to ScheduleLocalNotification, a copy is made and the objects won't test equal
        /// for cancellation.
        /// </summary>
        /// <param name="notification">Notification to cancel.</param>
        /// <param name="notificationIdKey">Key for ID in UserInfo of the UILocalNotification.</param>
        public void CancelLocalNotification(UILocalNotification notification, string notificationIdKey)
        {
            // set up action to cancel notification
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                try
                {
                    string idToCancel = notification.UserInfo.ValueForKey(new NSString(notificationIdKey)).ToString();

                    SensusServiceHelper.Get().Logger.Log("Cancelling local notification \"" + idToCancel + "\".", LoggingLevel.Normal, GetType());

                    // a local notification can be scheduled, in which case it hasn't yet been delivered and should reside within the shared 
                    // application's list of scheduled notifications. the tricky part here is that these notification objects aren't reference-equal, 
                    // so we can't just pass `notification` to CancelLocalNotification. instead, we must search for the notification by id and 
                    // cancel the appropriate scheduled notification object.
                    bool notificationCanceled = false;
                    foreach (UILocalNotification scheduledNotification in UIApplication.SharedApplication.ScheduledLocalNotifications)
                    {
                        string scheduledId = scheduledNotification.UserInfo.ValueForKey(new NSString(notificationIdKey))?.ToString();

                        if (scheduledId == idToCancel)
                        {
                            UIApplication.SharedApplication.CancelLocalNotification(scheduledNotification);
                            notificationCanceled = true;
                        }
                    }

                    // if we didn't cancel the notification above, then it isn't scheduled and should have already been delivered. if it has been 
                    // delivered, then our only option for cancelling it is to pass `notification` itself to CancelLocalNotification. this assumes
                    // that `notification` is the actual notification object and not, for example, the one originally passed to ScheduleLocalNotification.
                    if (!notificationCanceled)
                        UIApplication.SharedApplication.CancelLocalNotification(notification);
                }
                catch (Exception ex)
                {
                    SensusException.Report("Failed to cancel notification.", ex, false);
                }
            });
        }
    }
}