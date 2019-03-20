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
using Sensus.Notifications;
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
            string identifier = notification?.Request?.Identifier;

            SensusServiceHelper.Get().Logger.Log("Notification delivered:  " + (identifier ?? "[null identifier]"), LoggingLevel.Normal, GetType());

            // if the notification is for a callback, service the callback and do not show the notification. we use the local 
            // notification loop to schedule callback events and don't want to display the messages when the app is foregrounded.
            NSDictionary notificationInfo = notification?.Request?.Content?.UserInfo;
            iOSCallbackScheduler callbackScheduler = SensusContext.Current.CallbackScheduler as iOSCallbackScheduler;
            if (callbackScheduler.IsCallback(notificationInfo))
            {
                await callbackScheduler.ServiceCallbackAsync(notificationInfo);
                completionHandler?.Invoke(UNNotificationPresentationOptions.None);
            }
            else if (identifier == Notifier.PENDING_SURVEY_TEXT_NOTIFICATION_ID)
            {
                completionHandler?.Invoke(UNNotificationPresentationOptions.Alert | UNNotificationPresentationOptions.Badge | UNNotificationPresentationOptions.Sound);
            }
            else if (identifier == Notifier.PENDING_SURVEY_BADGE_NOTIFICATION_ID)
            {
                completionHandler?.Invoke(UNNotificationPresentationOptions.Badge);
            }
            else
            {
                completionHandler?.Invoke(UNNotificationPresentationOptions.Alert | UNNotificationPresentationOptions.Badge | UNNotificationPresentationOptions.Sound);
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

                // the user has responded. take action as appropriate.
                if (Enum.TryParse(notificationInfo?.ValueForKey(new NSString(Notifier.NOTIFICATION_USER_RESPONSE_ACTION_KEY)) as NSString, out NotificationUserResponseAction responseAction))
                {
                    notificationInfo.TryGetValue(new NSString(Notifier.NOTIFICATION_USER_RESPONSE_MESSAGE_KEY), out NSObject message);
                    await SensusContext.Current.Notifier.OnNotificationUserResponseAsync(responseAction, message?.ToString());
                }

                // provide some generic feedback if the user responded to a silent notification. this should only happen in race cases where
                // a silent notification is issued just before we enter background.
                if ((SensusContext.Current.CallbackScheduler as iOSCallbackScheduler).TryGetCallback(notificationInfo)?.Silent ?? false)
                {
                    await SensusServiceHelper.Get().FlashNotificationAsync("Sensus is running.");
                }
            }

            completionHandler?.Invoke();
        }
    }
}