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
using Foundation;
using Sensus.Context;
using Sensus.Exceptions;
using UIKit;
using UserNotifications;
using Sensus.Notifications;
using Sensus.iOS.Callbacks;
using Sensus.Callbacks;
using System.Threading.Tasks;

namespace Sensus.iOS.Notifications.UNUserNotifications
{
    public class UNUserNotificationNotifier : iOSNotifier
    {
        public override async Task IssueNotificationAsync(string title, string message, string id, Protocol protocol, bool alertUser, DisplayPage displayPage)
        {
            await IssueNotificationAsync(title, message, id, protocol, alertUser, displayPage, DateTime.Now, null, null);
        }

        public async Task IssueSilentNotificationAsync(string id, DateTime triggerDateTime, NSMutableDictionary info, Action<UNNotificationRequest> requestCreated = null)
        {
            // the user should never see a silent notification since we cancel them when the app is backgrounded. but there are race conditions that
            // might result in a silent notifiation being scheduled just before the app is backgrounded. give a generic message so that the notification
            // isn't totally confusing to the user.
            await IssueNotificationAsync("Please open this notification.", "One of your studies needs to be updated.", id, null, false, DisplayPage.None, triggerDateTime, info, requestCreated);
        }

        public async Task IssueNotificationAsync(string title, string message, string id, Protocol protocol, bool alertUser, DisplayPage displayPage, DateTime triggerDateTime, NSMutableDictionary info, Action<UNNotificationRequest> requestCreated = null)
        {
            // the callback scheduler will pass in an initialized user info (containing the callback id, invocation id, etc.), but 
            // other requests for notifications might not come with such information. initialize the user info if needed.
            if (info == null)
            {
                info = new NSMutableDictionary();
            }

            info.SetValueForKey(new NSString(id), new NSString(NOTIFICATION_ID_KEY));
            info.SetValueForKey(new NSString(displayPage.ToString()), new NSString(DISPLAY_PAGE_KEY));

            UNMutableNotificationContent content = new UNMutableNotificationContent
            {
                UserInfo = info
            };

            // the following properties are allowed to be null, but they cannot be set to null.

            if (!string.IsNullOrWhiteSpace(title))
            {
                content.Title = title;
            }

            if (!string.IsNullOrWhiteSpace(message))
            {
                content.Body = message;
            }

            // protocol might be null when issuing the pending surveys notification.
            if (alertUser && (protocol == null || !protocol.TimeIsWithinAlertExclusionWindow(triggerDateTime.TimeOfDay)))
            {
                content.Sound = UNNotificationSound.Default;
            }

            await IssueNotificationAsync(id, content, triggerDateTime, requestCreated);
        }

        public async Task IssueNotificationAsync(string id, UNNotificationContent content, DateTime triggerDateTime, Action<UNNotificationRequest> requestCreated = null)
        {
            UNCalendarNotificationTrigger trigger = null;

            // we're going to specify an absolute trigger date below. if this time is in the past by the time
            // the notification center processes it (race condition), then the notification will not be scheduled. 
            // so ensure that we leave some time to avoid the race condition by triggering an immediate notification
            // for any trigger date that is not greater than several seconds into the future.
            if (triggerDateTime > DateTime.Now + iOSCallbackScheduler.CALLBACK_NOTIFICATION_HORIZON_THRESHOLD)
            {
                NSDateComponents triggerDateComponents = new NSDateComponents
                {
                    Year = triggerDateTime.Year,
                    Month = triggerDateTime.Month,
                    Day = triggerDateTime.Day,
                    Hour = triggerDateTime.Hour,
                    Minute = triggerDateTime.Minute,
                    Second = triggerDateTime.Second,
                    Calendar = NSCalendar.CurrentCalendar,
                    TimeZone = NSTimeZone.LocalTimeZone
                };

                trigger = UNCalendarNotificationTrigger.CreateTrigger(triggerDateComponents, false);
            }

            UNNotificationRequest notificationRequest = UNNotificationRequest.FromIdentifier(id, content, trigger);
            requestCreated?.Invoke(notificationRequest);
            await IssueNotificationAsync(notificationRequest);
        }

        public async Task IssueNotificationAsync(UNNotificationRequest request)
        {
            // although we should never, we might be getting in null requests from somewhere. bail if we do.
            if (request == null)
            {
                SensusException.Report("Null notification request.");
                return;
            }

            // don't issue silent notifications from the background, as they will be displayed to the user upon delivery, and this will confuse the user (they're 
            // not designed to be seen). this can happen in a race condition where sensus transitions to the background but has a small amount of time to execute,
            // and in that time a silent callback (e.g., for local data store) is scheduled. checking for background state below will help mitigate this.
            bool abort = false;

            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                iOSCallbackScheduler callbackScheduler = SensusContext.Current.CallbackScheduler as iOSCallbackScheduler;
                ScheduledCallback callback = callbackScheduler.TryGetCallback(request?.Content?.UserInfo);
                abort = (callback?.Silent ?? false) && UIApplication.SharedApplication.ApplicationState == UIApplicationState.Background;
            });

            if (abort)
            {
                SensusServiceHelper.Get().Logger.Log("Aborting notification:  Will not issue silent notification from background.", LoggingLevel.Normal, GetType());
                return;
            }

            await UNUserNotificationCenter.Current.AddNotificationRequestAsync(request);

            SensusServiceHelper.Get().Logger.Log("Notification " + request.Identifier + " requested for " + ((request.Trigger as UNCalendarNotificationTrigger)?.NextTriggerDate.ToString() ?? "[time not specified]") + ".", LoggingLevel.Normal, GetType());
        }

        public override void CancelNotification(string id)
        {
            if (id == null)
            {
                return;
            }

            var ids = new[] { id };
            UNUserNotificationCenter.Current.RemoveDeliveredNotifications(ids);
            UNUserNotificationCenter.Current.RemovePendingNotificationRequests(ids);
        }

        public void CancelNotification(UNNotificationRequest request)
        {
            CancelNotification(request?.Identifier);
        }
    }
}
