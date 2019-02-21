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
            // isn't totally confusing to the user. furthermore, it appears that notifications must have content in order to come back.
            await IssueNotificationAsync("Notice", "Sensus is not currently running. You may safely ignore this notification if desired.", id, null, false, DisplayPage.None, triggerDateTime, info, requestCreated);
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
            info.SetValueForKey(NSNumber.FromBoolean(alertUser), new NSString(NOTIFICATION_ALERTING_KEY));

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

            await IssueNotificationAsync(id, content, protocol, triggerDateTime, requestCreated);
        }

        public async Task IssueNotificationAsync(string id, UNMutableNotificationContent content, Protocol protocol, DateTime triggerDateTime, Action<UNNotificationRequest> requestCreated = null)
        {
            // if the notification is configured to alert users and the trigger time doesn't fall within 
            // one of the protocol's alert exclusion windows, then set the sound.
            bool notificationIsAlerting = (content.UserInfo.ValueForKey(new NSString(NOTIFICATION_ALERTING_KEY)) as NSNumber).BoolValue;
            bool triggerIsWithinExclusionWindow = protocol?.TimeIsWithinAlertExclusionWindow(triggerDateTime.TimeOfDay) ?? false;

            if (notificationIsAlerting && !triggerIsWithinExclusionWindow)
            {
                content.Sound = UNNotificationSound.Default;
            }
            else
            {
                content.Sound = null;
            }

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

        public override void RemoveAllNotifications()
        {
            UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications();
            UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();
        }
    }
}