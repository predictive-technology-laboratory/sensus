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
using Sensus.Callbacks;
using Sensus.Context;
using Sensus.Exceptions;
using UserNotifications;

namespace Sensus.iOS.Callbacks.UNUserNotifications
{
    public class UNUserNotificationNotifier : iOSNotifier, IUNUserNotificationNotifier
    {
        public override void IssueNotificationAsync(string title, string message, string id, bool playSound, DisplayPage displayPage)
        {
            IssueNotificationAsync(title, message, id, playSound, displayPage, -1, null, null);
        }

        public void IssueSilentNotificationAsync(string id, int delayMS, NSMutableDictionary info, Action<UNNotificationRequest> requestCreated = null)
        {
            if (info == null)
                info = new NSMutableDictionary();

            info.SetValueForKey(new NSNumber(true), new NSString(SILENT_NOTIFICATION_KEY));

            IssueNotificationAsync("silent", "silent", id, false, DisplayPage.None, delayMS, info, requestCreated);
        }

        public void IssueNotificationAsync(string title, string message, string id, bool playSound, DisplayPage displayPage, int delayMS, NSMutableDictionary info, Action<UNNotificationRequest> requestCreated = null)
        {
            if (info == null)
                info = new NSMutableDictionary();

            info.SetValueForKey(new NSString(id), new NSString(NOTIFICATION_ID_KEY));
            info.SetValueForKey(new NSString(displayPage.ToString()), new NSString(DISPLAY_PAGE_KEY));

            UNMutableNotificationContent content = new UNMutableNotificationContent
            {
                UserInfo = info
            };

            // the following properties are allowed to be null, but they cannot be set to null.

            if (!string.IsNullOrWhiteSpace(title))
                content.Title = title;

            if (!string.IsNullOrWhiteSpace(message))
                content.Body = message;

            if (playSound)
                content.Sound = UNNotificationSound.Default;

            IssueNotificationAsync(id, content, delayMS, requestCreated);
        }

        public void IssueNotificationAsync(string id, UNNotificationContent content, double delayMS, Action<UNNotificationRequest> requestCreated = null)
        {
            UNCalendarNotificationTrigger trigger = null;

            // a negative delay indicates an immediate notification, which is achieved with a null trigger.
            if (delayMS > 0)
            {
                // we're going to specify an absolute date below based on the current time and the given delay. if this time is in the past by the time
                // the notification center processes it (race condition), then the notification will not be scheduled. so ensure that we leave some time
                // and avoid the race condition.
                if (delayMS < 5000)
                    delayMS = 5000;

                DateTime triggerDateTime = DateTime.Now.AddMilliseconds(delayMS);
                NSDateComponents triggerDateComponents = new NSDateComponents
                {
                    Year = triggerDateTime.Year,
                    Month = triggerDateTime.Month,
                    Day = triggerDateTime.Day,
                    Hour = triggerDateTime.Hour,
                    Minute = triggerDateTime.Minute,
                    Second = triggerDateTime.Second
                };

                trigger = UNCalendarNotificationTrigger.CreateTrigger(triggerDateComponents, false);
            }

            UNNotificationRequest notificationRequest = UNNotificationRequest.FromIdentifier(id, content, trigger);
            requestCreated?.Invoke(notificationRequest);
            IssueNotificationAsync(notificationRequest);
        }

        public void IssueNotificationAsync(UNNotificationRequest request, Action<NSError> errorCallback = null)
        {
            UNUserNotificationCenter.Current.AddNotificationRequest(request, async error =>
            {
                if (error == null)
                {
                    SensusServiceHelper.Get().Logger.Log("Notification " + request.Identifier + " requested for " + ((request.Trigger as UNCalendarNotificationTrigger)?.NextTriggerDate.ToString() ?? "[time not specified]") + ".", LoggingLevel.Normal, GetType());
                }
                else
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to add notification request:  " + error.Description, LoggingLevel.Normal, GetType());
                    SensusException.Report("Failed to add notification request:  " + error.Description);
                }

                UNNotificationRequest[] pendingRequests = await UNUserNotificationCenter.Current.GetPendingNotificationRequestsAsync();
                SensusServiceHelper.Get().Logger.Log("Pending notification requests:  " + pendingRequests.Length, LoggingLevel.Normal, GetType());
                foreach (UNNotificationRequest pendingRequest in pendingRequests)
                    SensusServiceHelper.Get().Logger.Log($"{pendingRequest.Identifier}:  {(pendingRequest.Trigger as UNCalendarNotificationTrigger)?.NextTriggerDate}", LoggingLevel.Normal, GetType());

                errorCallback?.Invoke(error);
            });
        }

        public override void CancelNotification(string id)
        {
            var ids = new[] { id };
            UNUserNotificationCenter.Current.RemoveDeliveredNotifications(ids);
            UNUserNotificationCenter.Current.RemovePendingNotificationRequests(ids);
        }

        public void CancelNotification(UNNotificationRequest request)
        {
            CancelNotification(request.Identifier);
        }

        public override void CancelSilentNotifications()
        {
            UNUserNotificationCenter.Current.GetPendingNotificationRequests(requests =>
            {
                foreach (UNNotificationRequest request in requests)
                {
                    if ((request.Content?.UserInfo?.ValueForKey(new NSString(SILENT_NOTIFICATION_KEY)) as NSNumber)?.BoolValue ?? false)
                    {
                        CancelNotification(request);
                    }
                }
            });
        }
    }
}