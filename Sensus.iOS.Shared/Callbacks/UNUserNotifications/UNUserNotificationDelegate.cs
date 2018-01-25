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
using UserNotifications;
using UIKit;

namespace Sensus.iOS.Callbacks.UNUserNotifications
{
    public class UNUserNotificationDelegate : UNUserNotificationCenterDelegate
    {
        public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            SensusServiceHelper.Get().Logger.Log("Notification delivered:  " + (notification?.Request?.Identifier ?? "[null identifier]"), LoggingLevel.Normal, GetType());

            // don't alert the user for callback notifications presented when the app is not in the background state.
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                if(iOSCallbackScheduler.IsCallback(notification?.Request?.Content?.UserInfo) && UIApplication.SharedApplication.ApplicationState != UIApplicationState.Background)
                {
                    completionHandler(UNNotificationPresentationOptions.None);                    
                }
                else
                {
                    completionHandler(UNNotificationPresentationOptions.Alert);
                }
            });

            // common scenario:  app is backgrounded, and multiple non-silent sensus notifications appear in the iOS tray. the user taps one of these, which
            // dismisses the tapped notification and brings up sensus. upon activation sensus then updates and reissues all notifications. these reissued
            // notifications will come directly to the app as long as it's in the foreground. the original notifications that were in the iOS notification
            // tray will still be there, despite the fact that the notifications have been sent to the app via the current method. short story:  we need to 
            // cancel each notification as it comes in to remove it from the notification center.
            SensusContext.Current.Notifier.CancelNotification(notification?.Request?.Identifier);

            (SensusContext.Current.CallbackScheduler as IiOSCallbackScheduler)?.ServiceCallbackAsync(notification?.Request?.Content?.UserInfo);
        }

        public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            UNNotificationRequest request = response?.Notification?.Request;
            NSDictionary notificationInfo = request?.Content?.UserInfo;

            if (notificationInfo != null)
            {
                SensusServiceHelper.Get().Logger.Log("Notification received user response:  " + (request.Identifier ?? "[null identifier]"), LoggingLevel.Normal, GetType());

                (SensusContext.Current.CallbackScheduler as IiOSCallbackScheduler)?.OpenDisplayPage(notificationInfo);

                // provide some generic feedback if the user responded to a silent notification. this should only happen in race cases where
                // a silent notification is issued just before we enter background.
                if ((notificationInfo.ValueForKey(new NSString(iOSNotifier.SILENT_NOTIFICATION_KEY)) as NSNumber)?.BoolValue ?? false)
                {
                    SensusServiceHelper.Get().FlashNotificationAsync("Study Updated.");
                }
            }

            completionHandler();
        }
    }
}