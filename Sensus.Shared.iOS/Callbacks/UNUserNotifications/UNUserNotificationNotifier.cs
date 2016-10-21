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
using Sensus.Shared.Exceptions;
using UserNotifications;

namespace Sensus.Shared.iOS.Callbacks.UNUserNotifications
{
    public class UNUserNotificationNotifier : iOSNotifier, IUNUserNotificationNotifier
    {
        public override void IssueNotificationAsync(string title, string message, string id, bool playSound, DisplayPage displayPage)
        {
            IssueNotificationAsync(title, message, id, playSound, displayPage, 1, null, null); // delay must be > 0
        }

        public void IssueSilentNotificationAsync(string id, int delayMS, NSMutableDictionary notificationInfo, Action<UNNotificationRequest> requestCallback = null)
        {
            if (notificationInfo == null)
                notificationInfo = new NSMutableDictionary();

            notificationInfo.SetValueForKey(new NSNumber(true), new NSString(SILENT_NOTIFICATION_KEY));

            IssueNotificationAsync("silent", "silent", id, false, DisplayPage.None, delayMS, notificationInfo, requestCallback);
        }

        public void IssueNotificationAsync(string title, string message, string id, bool playSound, DisplayPage displayPage, int delayMS, NSMutableDictionary notificationInfo, Action<UNNotificationRequest> requestCallback = null)
        {
            if (notificationInfo == null)
                notificationInfo = new NSMutableDictionary();

            notificationInfo.SetValueForKey(new NSString(id), new NSString(NOTIFICATION_ID_KEY));
            notificationInfo.SetValueForKey(new NSString(displayPage.ToString()), new NSString(DISPLAY_PAGE_KEY));

            UNMutableNotificationContent notificationContent = new UNMutableNotificationContent
            {
                UserInfo = notificationInfo
            };

            // the following properties are allowed to be null, but they cannot be set to null.

            if (!string.IsNullOrWhiteSpace(title))
                notificationContent.Title = title;

            if (!string.IsNullOrWhiteSpace(message))
                notificationContent.Body = message;

            if (playSound)
                notificationContent.Sound = UNNotificationSound.Default;

            // delay must be > 0 or exception will be thrown
            if (delayMS <= 0)
                delayMS = 1;

            UNTimeIntervalNotificationTrigger notificationTrigger = UNTimeIntervalNotificationTrigger.CreateTrigger(delayMS / 1000d, false);
            UNNotificationRequest notificationRequest = UNNotificationRequest.FromIdentifier(id, notificationContent, notificationTrigger);
            requestCallback?.Invoke(notificationRequest);
            IssueNotificationAsync(notificationRequest);
        }

        public void IssueNotificationAsync(UNNotificationRequest request, Action<NSError> errorCallback = null)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                UNUserNotificationCenter.Current.AddNotificationRequest(request, error =>
                {
                    if (error == null)
                    {
                        SensusServiceHelper.Get().Logger.Log("Notification " + request.Identifier + " requested for " + (request.Trigger as UNTimeIntervalNotificationTrigger).NextTriggerDate + ". ", LoggingLevel.Normal, GetType());
                    }
                    else
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to add notification request:  " + error.Description, LoggingLevel.Normal, GetType());
                        SensusException.Report("Failed to add notification request:  " + error.Description);
                    }

                    errorCallback?.Invoke(error);
                });
            });
        }

        public override void CancelNotification(string id)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                var ids = new[] { id };
                UNUserNotificationCenter.Current.RemoveDeliveredNotifications(ids);
                UNUserNotificationCenter.Current.RemovePendingNotificationRequests(ids);
            });
        }

        public void CancelNotification(UNNotificationRequest request)
        {
            CancelNotification(request.Identifier);
        }

        public override void CancelSilentNotifications()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
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
            });
        }
    }
}