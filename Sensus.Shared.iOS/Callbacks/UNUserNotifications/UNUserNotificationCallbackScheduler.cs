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
using System.Collections.Generic;
using Foundation;
using Sensus.Shared.Callbacks;
using Sensus.Shared.Context;
using UserNotifications;

namespace Sensus.Shared.iOS.Callbacks.UNUserNotifications
{
    public class UNUserNotificationCallbackScheduler : iOSCallbackScheduler
    {
        private Dictionary<string, UNNotificationRequest> _callbackIdRequest;

        public UNUserNotificationCallbackScheduler()
        {
            _callbackIdRequest = new Dictionary<string, UNNotificationRequest>();
        }

        protected override void ScheduleCallbackAsync(string callbackId, int delayMS, bool repeating, int repeatDelayMS, bool repeatLag)
        {
            string userNotificationMessage = GetCallbackUserNotificationMessage(callbackId);
            string notificationId = GetCallbackNotificationId(callbackId);

            NSDictionary callbackInfo = GetCallbackInfo(callbackId, repeating, repeatDelayMS, repeatLag, notificationId, SensusContext.Current.ActivationId);

            // user info can be null (e.g., if we don't have an activation ID). don't schedule the notification if this happens.
            if (callbackInfo == null)
                return;

            (SensusContext.Current.Notifier as IUNUserNotificationNotifier).IssueNotificationAsync(userNotificationMessage, callbackId, true, null, delayMS, callbackInfo, (request, error) =>
            {
                if (error == null)
                {
                    lock (_callbackIdRequest)
                    {
                        _callbackIdRequest.Add(callbackId, request);
                    }
                }
            });
        }

        public override void UpdateCallbackActivationIdsAsync(string newActivationId)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                // since all notifications are about to be rescheduled, clear all current notifications.
                UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications();
                UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();

                // see corresponding comments in UILocalNotificationCallbackScheduler
                lock (_callbackIdRequest)
                {
                    foreach (string callbackId in _callbackIdRequest.Keys)
                    {
                        UNNotificationRequest request = _callbackIdRequest[callbackId];

                        if (request.Content.UserInfo != null)
                        {
                            string activationId = (request.Content.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_ACTIVATION_ID_KEY)) as NSString).ToString();
                            if (activationId != newActivationId)
                            {
                                // reset the UserInfo to include the current activation ID
                                bool repeating = (request.Content.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEATING_KEY)) as NSNumber).BoolValue;
                                int repeatDelayMS = (request.Content.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEAT_DELAY_KEY)) as NSNumber).Int32Value;
                                bool repeatLag = (request.Content.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEAT_LAG_KEY)) as NSNumber).BoolValue;
                                string notificationId = (request.Content.UserInfo.ValueForKey(new NSString(Notifier.NOTIFICATION_ID_KEY)) as NSString)?.ToString();
                                (request.Content as UNMutableNotificationContent).UserInfo = GetCallbackInfo(callbackId, repeating, repeatDelayMS, repeatLag, notificationId, newActivationId);

                                // since we set the UILocalNotification's FireDate when it was constructed, if it's currently in the past it will fire immediately 
                                // when scheduled again with the new activation ID.
                                if (request.Content.UserInfo != null)
                                    (SensusContext.Current.Notifier as IUNUserNotificationNotifier).IssueNotificationAsync(request);
                            }
                        }
                    }
                }
            });
        }

        public override void RaiseCallbackAsync(string callbackId, bool repeating, int repeatDelayMS, bool repeatLag, bool notifyUser, Action<DateTime> scheduleRepeatCallback, Action letDeviceSleepCallback, Action finishedCallback)
        {
            // see corresponding comments in UILocalNotificationCallbackScheduler

            UNNotificationRequest request;
            lock (_callbackIdRequest)
            {
                request = _callbackIdRequest[callbackId];
                _callbackIdRequest.Remove(callbackId);
            }

            base.RaiseCallbackAsync(callbackId, repeating, repeatDelayMS, repeatLag, notifyUser,

            repeatCallbackTime =>
            {
                lock (_callbackIdRequest)
                {
                    SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                    {
                        UNTimeIntervalNotificationTrigger callbackNotificationTrigger = UNTimeIntervalNotificationTrigger.CreateTrigger((DateTime.Now - repeatCallbackTime).TotalDays, false);
                        UNNotificationRequest callbackNotificationRequest = UNNotificationRequest.FromIdentifier(request.Identifier, request.Content, callbackNotificationTrigger);

                        (SensusContext.Current.Notifier as IUNUserNotificationNotifier).IssueNotificationAsync(callbackNotificationRequest, error =>
                        {
                            if (error == null)
                            {
                                lock (_callbackIdRequest)
                                {
                                    _callbackIdRequest.Add(callbackNotificationRequest.Identifier, callbackNotificationRequest);
                                }
                            }
                        });
                    });
                }
            },

            letDeviceSleepCallback, finishedCallback);
        }

        protected override void UnscheduleCallbackPlatformSpecific(string callbackId)
        {
            lock (_callbackIdRequest)
            {
                // there are race conditions on this collection, and the key might be removed elsewhere
                UNNotificationRequest request;
                if (_callbackIdRequest.TryGetValue(callbackId, out request))
                {
                    (SensusContext.Current.Notifier as IUNUserNotificationNotifier)?.CancelNotification(request);
                    _callbackIdRequest.Remove(callbackId);
                }
            }
        }
    }
}