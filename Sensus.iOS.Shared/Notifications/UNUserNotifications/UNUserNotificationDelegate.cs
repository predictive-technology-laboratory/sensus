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
using Sensus.iOS.Callbacks;
using UserNotifications;

namespace Sensus.iOS.Notifications.UNUserNotifications
{
    public class UNUserNotificationDelegate : UNUserNotificationCenterDelegate
    {
        /// <summary>
        /// Called just prior to a notification being presented while the app is in the foreground.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="notification"></param>
        /// <param name="completionHandler"></param>
        public override async void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            SensusServiceHelper.Get().Logger.Log("Notification delivered:  " + (notification?.Request?.Identifier ?? "[null identifier]"), LoggingLevel.Normal, GetType());

            // long story:  app is backgrounded, and multiple non-silent sensus notifications appear in the iOS tray. the user taps one of these, which
            // dismisses the tapped notification and brings up sensus. upon activation sensus then updates and reissues all notifications. these reissued
            // notifications will come directly to the app as long as it's in the foreground. the original notifications that were in the iOS notification
            // tray will still be there, despite the fact that the notifications have been sent to the app via the current method. short story:  we need to 
            // cancel each notification as it comes in to remove it from the notification center.
            SensusContext.Current.Notifier.CancelNotification(notification?.Request?.Identifier);

            // if the notification is for a callback, service the callback and do not show the notification.
            NSDictionary notificationInfo = notification?.Request?.Content?.UserInfo;
            iOSCallbackScheduler callbackScheduler = SensusContext.Current.CallbackScheduler as iOSCallbackScheduler;
            if (callbackScheduler.IsCallback(notificationInfo))
            {
                await callbackScheduler.ServiceCallbackAsync(notificationInfo);
                completionHandler?.Invoke(UNNotificationPresentationOptions.None);
            }

            // if the notification is for pending surveys or study updates, show the notification along with any alert or sound.
            if (notification?.Request?.Identifier == SensusServiceHelper.PENDING_SURVEY_NOTIFICATION_ID ||
                notification?.Request?.Identifier == SensusServiceHelper.PROTOCOL_UPDATED_NOTIFICATION_ID)
            {
                completionHandler?.Invoke(UNNotificationPresentationOptions.Alert | UNNotificationPresentationOptions.Sound);
            }
        }

        /// <summary>
        /// Called when the user taps a notification.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="response"></param>
        /// <param name="completionHandler"></param>
        public override async void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            UNNotificationRequest request = response?.Notification?.Request;
            NSDictionary notificationInfo = request?.Content?.UserInfo;

            if (notificationInfo != null)
            {
                SensusServiceHelper.Get().Logger.Log("Notification received user response:  " + (request.Identifier ?? "[null identifier]"), LoggingLevel.Normal, GetType());

                // if the notification is associated with a particular UI page to display, show that page now.
                iOSCallbackScheduler callbackScheduler = SensusContext.Current.CallbackScheduler as iOSCallbackScheduler;
                callbackScheduler.OpenDisplayPage(notificationInfo);

                // provide some generic feedback if the user responded to a silent notification. this should only happen in race cases where
                // a silent notification is issued just before we enter background.
                if (callbackScheduler.TryGetCallback(notificationInfo)?.Silent ?? false)
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync("Study Updated.");
                }
            }

            completionHandler();
        }
    }
}