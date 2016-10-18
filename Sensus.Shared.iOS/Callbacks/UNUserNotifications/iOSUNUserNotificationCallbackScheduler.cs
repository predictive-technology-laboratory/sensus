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
using UserNotifications;

namespace Sensus.Shared.iOS
{
    public class iOSUNUserNotificationCallbackScheduler : iOSCallbackScheduler
    {
        public iOSUNUserNotificationCallbackScheduler()
        {
        }

        public override void UpdateCallbackActivationIdsAsync(string newActivationId)
        {
            throw new NotImplementedException();
        }

        protected override void ScheduleCallbackAsync(string callbackId, int delayMS, bool repeating, int repeatDelayMS, bool repeatLag)
        {
                    /*UNMutableNotificationContent content = new UNMutableNotificationContent
                    {
                        Title = "Sensus",
                        UserInfo = GetNotificationUserInfoDictionary(callbackId, repeating, repeatDelayMS, repeatLag, notificationId)
                    };

                    if (userNotificationMessage != null)
                        content.Body = userNotificationMessage;

                    // user info can be null if we don't have an activation ID. don't schedule the notification if this happens.
                    if (content.UserInfo == null)
                        return;

                    lock (_callbackIdNotification)
                    {
                        //_callbackIdNotification.Add(callbackId, notification);
                    }

                    if (userNotificationMessage != null)
                        content.Sound = UNNotificationSound.Default;

                    UNTimeIntervalNotificationTrigger trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(delayMS / 1000d, false);

                    UNNotificationRequest request = UNNotificationRequest.FromIdentifier(callbackId, content, trigger);

                    UNUserNotificationCenter.Current.AddNotificationRequest(request, error =>
                    {
                    });

                    Logger.Log("Callback " + callbackId + " scheduled for " + trigger.NextTriggerDate + " (" + (repeating ? "repeating" : "one-time") + "). " + _callbackIdNotification.Count + " total callbacks in iOS service helper.", LoggingLevel.Normal, GetType());*/
        }

        protected override void UnscheduleCallbackPlatformSpecific(string callbackId)
        {
            throw new NotImplementedException();
        }
    }
}
