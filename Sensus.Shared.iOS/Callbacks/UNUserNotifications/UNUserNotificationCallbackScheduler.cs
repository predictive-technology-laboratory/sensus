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
using System.Linq;
using Foundation;
using Sensus.Shared.Callbacks;
using Sensus.Shared.Context;
using UserNotifications;
using Xamarin.Forms.Platform.iOS;

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
            // get the callback information. this can be null if we don't have all required information. don't schedule the notification if this happens.
            DisplayPage displayPage = GetCallbackDisplayPage(callbackId);
            NSMutableDictionary callbackInfo = GetCallbackInfo(callbackId, repeating, repeatDelayMS, repeatLag, displayPage, SensusContext.Current.ActivationId);
            if (callbackInfo == null)
                return;

            string userNotificationMessage = GetCallbackUserNotificationMessage(callbackId);

            Action<UNNotificationRequest> requestCreated = request =>
            {
                lock (_callbackIdRequest)
                {
                    _callbackIdRequest.Add(callbackId, request);
                }
            };

            IUNUserNotificationNotifier notifier = SensusContext.Current.Notifier as IUNUserNotificationNotifier;

            if (userNotificationMessage == null)
                notifier.IssueSilentNotificationAsync(callbackId, delayMS, callbackInfo, requestCreated);
            else
                notifier.IssueNotificationAsync("Sensus", userNotificationMessage, callbackId, true, displayPage, delayMS, callbackInfo, requestCreated);
        }

        public override void UpdateCallbackActivationIds(string newActivationId)
        {
            // see corresponding comments in UILocalNotificationCallbackScheduler
            lock (_callbackIdRequest)
            {
                foreach (string callbackId in _callbackIdRequest.Keys.ToList())
                {
                    UNNotificationRequest request = _callbackIdRequest[callbackId];
                    string activationId = (request.Content.UserInfo?.ValueForKey(new NSString(SENSUS_CALLBACK_ACTIVATION_ID_KEY)) as NSString)?.ToString();
                    if (activationId != newActivationId)
                    {
                        double delayMS = 0;
                        DateTime? triggerDateTime = (request.Trigger as UNCalendarNotificationTrigger)?.NextTriggerDate?.ToDateTime().ToLocalTime();
                        if (triggerDateTime.HasValue)
                            delayMS = (triggerDateTime.Value - DateTime.Now).TotalMilliseconds;

                        UNMutableNotificationContent newContent = request.Content.MutableCopy() as UNMutableNotificationContent;
                        NSMutableDictionary newUserInfo = new NSMutableDictionary(request.Content.UserInfo);
                        newUserInfo.SetValueForKey(new NSString(newActivationId), new NSString(SENSUS_CALLBACK_ACTIVATION_ID_KEY));
                        newContent.UserInfo = newUserInfo;

                        // we don't need to cancel the current notification. reissuing with the same id will update it.
                        (SensusContext.Current.Notifier as IUNUserNotificationNotifier).IssueNotificationAsync(callbackId, newContent, delayMS, newRequest =>
                        {
                            _callbackIdRequest[callbackId] = newRequest;
                        });
                    }
                }
            }
        }

        public override void RaiseCallbackAsync(string callbackId, bool repeating, int repeatDelayMS, bool repeatLag, bool notifyUser, Action<DateTime> scheduleRepeatCallback, Action letDeviceSleepCallback, Action finishedCallback)
        {
            // see corresponding comments in UILocalNotificationCallbackScheduler

            UNNotificationRequest request;
            lock (_callbackIdRequest)
            {
                _callbackIdRequest.TryGetValue(callbackId, out request);
                _callbackIdRequest.Remove(callbackId);
            }

            base.RaiseCallbackAsync(callbackId, repeating, repeatDelayMS, repeatLag, notifyUser,

            repeatCallbackTime =>
            {
                (SensusContext.Current.Notifier as IUNUserNotificationNotifier).IssueNotificationAsync(request.Identifier, request.Content, (repeatCallbackTime - DateTime.Now).TotalMilliseconds, newRequest =>
                {
                    lock (_callbackIdRequest)
                    {
                        _callbackIdRequest.Add(newRequest.Identifier, newRequest);
                    }
                });
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