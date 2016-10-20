﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
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
using Foundation;
using Sensus.Shared.Callbacks;
using Sensus.Shared.Context;
using UIKit;
using Xamarin.Forms.Platform.iOS;

namespace Sensus.Shared.iOS.Callbacks.UILocalNotifications
{
    public class UILocalNotificationCallbackScheduler : iOSCallbackScheduler
    {
        private Dictionary<string, UILocalNotification> _callbackIdNotification;

        public UILocalNotificationCallbackScheduler()
        {
            _callbackIdNotification = new Dictionary<string, UILocalNotification>();
        }

        protected override void ScheduleCallbackAsync(string callbackId, int delayMS, bool repeating, int repeatDelayMS, bool repeatLag)
        {
            // the following lines need to precede the run on main thread to avoid deadlocks -- this is an old comment. not sure if it's still the case.
            string userNotificationMessage = GetCallbackUserNotificationMessage(callbackId);
            string notificationId = GetCallbackNotificationId(callbackId);

            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                NSDictionary callbackInfo = GetCallbackInfo(callbackId, repeating, repeatDelayMS, repeatLag, notificationId, SensusContext.Current.ActivationId);

                // user info can be null (e.g., if we don't have an activation ID). don't schedule the notification if this happens.
                if (callbackInfo == null)
                    return;

                // all properties below were introduced in iOS 8.0. we currently target 8.0 and above, so these should be safe to set.
                UILocalNotification callbackNotification = new UILocalNotification
                {
                    FireDate = DateTime.UtcNow.AddMilliseconds(delayMS).ToNSDate(),
                    TimeZone = null,  // null for UTC interpretation of FireDate
                    AlertBody = userNotificationMessage,
                    UserInfo = callbackInfo
                };

                // also in 8.0
                if (userNotificationMessage != null)
                    callbackNotification.SoundName = UILocalNotification.DefaultSoundName;

                // the following UILocalNotification property was introduced in iOS 8.2:  https://developer.apple.com/reference/uikit/uilocalnotification/1616647-alerttitle
                if (UIDevice.CurrentDevice.CheckSystemVersion(8, 2))
                    callbackNotification.AlertTitle = "Sensus";

                ScheduleCallbackNotification(callbackNotification, callbackId);

                SensusServiceHelper.Get().Logger.Log("Callback " + callbackId + " scheduled for " + callbackNotification.FireDate + " (" + (repeating ? "repeating" : "one-time") + "). " + _callbackIdNotification.Count + " total callbacks in scheduler.", LoggingLevel.Normal, GetType());
            });
        }

        public override void RaiseCallbackAsync(string callbackId, bool repeating, int repeatDelayMS, bool repeatLag, bool notifyUser, Action<DateTime> scheduleRepeatCallback, Action letDeviceSleepCallback, Action finishedCallback)
        {
            // remove from platform-specific notification collection before raising the callback. the purpose of the platform-specific notification collection 
            // is to hold the notifications between successive activations of the app. when the app is reactivated, notifications from this collection are 
            // updated with the new activation id and they are rescheduled. if, in raising the callback associated with the current notification, the app is 
            // reactivated (e.g., by a call to the facebook probe login manager), then the current notification will be reissued when updated via app reactivation 
            // (which will occur, e.g., when the facebook login manager returns control to the app). this can lead to duplicate notifications for the same callback, 
            // or infinite cycles of app reactivation if the notification raises a callback that causes it to be reissued (e.g., in the case of facebook login).
            UILocalNotification callbackNotification;
            lock (_callbackIdNotification)
            {
                callbackNotification = _callbackIdNotification[callbackId];
                _callbackIdNotification.Remove(callbackId);
            }

            base.RaiseCallbackAsync(callbackId, repeating, repeatDelayMS, repeatLag, notifyUser,

            repeatCallbackTime =>
            {
                lock (_callbackIdNotification)
                {
                    SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                    {
                        callbackNotification.FireDate = repeatCallbackTime.ToUniversalTime().ToNSDate();

                        ScheduleCallbackNotification(callbackNotification, callbackId);
                    });
                }
            },

            letDeviceSleepCallback, finishedCallback);
        }

        private void ScheduleCallbackNotification(UILocalNotification callbackNotification, string callbackId)
        {
            // add to the platform-specific notification collection, so that the notification is updated and reissued if/when the app is reactivated
            lock (_callbackIdNotification)
            {
                _callbackIdNotification.Add(callbackId, callbackNotification);
            }

            UIApplication.SharedApplication.ScheduleLocalNotification(callbackNotification);
        }

        public override void UpdateCallbackActivationIdsAsync(string newActivationId)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                // since all notifications are about to be rescheduled, clear all current notifications.
                UIApplication.SharedApplication.CancelAllLocalNotifications();
                UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;

                // this method will be called in one of three conditions:  (1) after sensus has been started and is running, (2)
                // after sensus has been reactivated and was already running, and (3) after a start attempt was made but failed.
                // in all three situations, there will be zero or more notifications present in the _callbackIdNotification lookup.
                // in (1), the notifications will have just been created and will have activation IDs set to the activation ID of
                // the current object. in (2), the notifications will have stale activation IDs. in (3), there will be no notifications.
                // the required post-condition of this method is that any present notification objects have activation IDs set to
                // the activation ID of the current object. so...let's make that happen.
                lock (_callbackIdNotification)
                {
                    foreach (string callbackId in _callbackIdNotification.Keys)
                    {
                        UILocalNotification notification = _callbackIdNotification[callbackId];

                        if (notification.UserInfo != null)
                        {
                            // get activation ID and check for condition (2) above
                            string activationId = (notification.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_ACTIVATION_ID_KEY)) as NSString).ToString();
                            if (activationId != newActivationId)
                            {
                                // reset the UserInfo to include the current activation ID
                                bool repeating = (notification.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEATING_KEY)) as NSNumber).BoolValue;
                                int repeatDelayMS = (notification.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEAT_DELAY_KEY)) as NSNumber).Int32Value;
                                bool repeatLag = (notification.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEAT_LAG_KEY)) as NSNumber).BoolValue;
                                string notificationId = (notification.UserInfo.ValueForKey(new NSString(Notifier.NOTIFICATION_ID_KEY)) as NSString)?.ToString();
                                notification.UserInfo = GetCallbackInfo(callbackId, repeating, repeatDelayMS, repeatLag, notificationId, newActivationId);

                                // since we set the UILocalNotification's FireDate when it was constructed, if it's currently in the past it will fire immediately 
                                // when scheduled again with the new activation ID.
                                if (notification.UserInfo != null)
                                    UIApplication.SharedApplication.ScheduleLocalNotification(notification);
                            }
                        }
                    }
                }
            });
        }

        protected override void UnscheduleCallbackPlatformSpecific(string callbackId)
        {
            lock (_callbackIdNotification)
            {
                // there are race conditions on this collection, and the key might be removed elsewhere
                UILocalNotification notification;
                if (_callbackIdNotification.TryGetValue(callbackId, out notification))
                {
                    (SensusContext.Current.Notifier as UILocalNotificationNotifier)?.CancelNotification(notification, SENSUS_CALLBACK_ID_KEY);
                    _callbackIdNotification.Remove(callbackId);
                }
            }
        }
    }
}