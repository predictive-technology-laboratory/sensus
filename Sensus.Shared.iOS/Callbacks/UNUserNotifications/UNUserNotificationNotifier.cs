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
using Sensus.Shared.Exceptions;
using UserNotifications;

namespace Sensus.Shared.iOS.Callbacks.UNUserNotifications
{
    public class UNUserNotificationNotifier : Notifier, IUNUserNotificationNotifier
    {
        public override void IssueNotificationAsync(string message, string id, bool playSound, bool vibrate)
        {
            IssueNotificationAsync(message, id, playSound, "Sensus", 0, null, null);
        }

        public void IssueNotificationAsync(string message, string id, bool playSound, string title, int delayMS, NSDictionary notificationInfo, Action<UNNotificationRequest, NSError> callback = null)
        {
            UNMutableNotificationContent notificationContent = new UNMutableNotificationContent();

            if (notificationInfo != null)
                notificationContent.UserInfo = notificationInfo;

            notificationContent.Title = title ?? "Empty";

            notificationContent.Body = message ?? "Empty";

            if (!string.IsNullOrWhiteSpace(notificationContent.Body) && playSound)
                notificationContent.Sound = UNNotificationSound.Default;

            UNTimeIntervalNotificationTrigger notificationTrigger = UNTimeIntervalNotificationTrigger.CreateTrigger(delayMS / 1000d, false);
            UNNotificationRequest notificationRequest = UNNotificationRequest.FromIdentifier(id, notificationContent, notificationTrigger);
            IssueNotificationAsync(notificationRequest, error => callback?.Invoke(notificationRequest, error));
        }

        public void IssueNotificationAsync(UNNotificationRequest request, Action<NSError> callback = null)
        {
            // remove previous notification with same ID
            var id = new[] { request.Identifier };
            UNUserNotificationCenter.Current.RemoveDeliveredNotifications(id);
            UNUserNotificationCenter.Current.RemovePendingNotificationRequests(id);

            // issue new notification
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

                callback?.Invoke(error);
            });
        }

        public void CancelNotification(string id)
        {
            var ids = new[] { id };
            UNUserNotificationCenter.Current.RemoveDeliveredNotifications(ids);
            UNUserNotificationCenter.Current.RemovePendingNotificationRequests(ids);
        }

        public void CancelNotification(UNNotificationRequest request)
        {
            CancelNotification(request.Identifier);
        }
    }
}
