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
using Sensus.Shared.Exceptions;
using Sensus.Shared.Notifications;
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

        protected override void ScheduleCallbackAsync(INotifyMeta meta)
        {
            throw new NotImplementedException();
            //string userNotificationMessage = GetCallbackUserNotificationMessage(callbackId);
            //string notificationId = GetCallbackNotificationId(callbackId);

            //NSDictionary callbackInfo = GetCallbackInfo(callbackId, repeating, repeatDelayMS, repeatLag, notificationId, SensusContext.Current.ActivationId);

            //// user info can be null (e.g., if we don't have an activation ID). don't schedule the notification if this happens.
            //if (callbackInfo == null)
            //    return;

            //UNMutableNotificationContent callbackNotificationContent = new UNMutableNotificationContent
            //{
            //    Title = "Sensus",
            //    UserInfo = callbackInfo
            //};

            //if (userNotificationMessage != null)
            //{
            //    callbackNotificationContent.Body = userNotificationMessage;
            //    callbackNotificationContent.Sound = UNNotificationSound.Default;
            //}

            //UNTimeIntervalNotificationTrigger callbackNotificationTrigger = UNTimeIntervalNotificationTrigger.CreateTrigger(delayMS / 1000d, false);
            //UNNotificationRequest callbackNotificationRequest = UNNotificationRequest.FromIdentifier(callbackId, callbackNotificationContent, callbackNotificationTrigger);
            //AddCallbackNotificationRequest(callbackNotificationRequest, repeating, true);
        }

        private void AddCallbackNotificationRequest(UNNotificationRequest request, bool repeating, bool addToCollectionIfSuccessful)
        {
            UNUserNotificationCenter.Current.AddNotificationRequest(request, error =>
            {
                if (error == null)
                {
                    if (addToCollectionIfSuccessful)
                    {
                        lock (_callbackIdRequest)
                        {
                            _callbackIdRequest.Add(request.Identifier, request);
                            SensusServiceHelper.Get().Logger.Log("Callback " + request.Identifier + " scheduled for " + (request.Trigger as UNTimeIntervalNotificationTrigger).NextTriggerDate + " (" + (repeating ? "repeating" : "one-time") + "). " + _callbackIdRequest.Count + " total callbacks in scheduler.", LoggingLevel.Normal, GetType());
                        }
                    }
                }
                else
                {
                    SensusServiceHelper.Get().Logger.Log("Failed to add notification request:  " + error.Description, LoggingLevel.Normal, GetType());
                    SensusException.Report("Failed to add notification request:  " + error.Description);
                }
            });
        }

        public override void RaiseCallbackAsync(INotifyMeta meta, bool notifyUser, Action<DateTime> scheduleRepeatCallback, Action letDeviceSleepCallback, Action finishedCallback)
        {
            // see corresponding comment in UILocalNotificationCallbackScheduler
            UNNotificationRequest request;
            lock (_callbackIdRequest)
            {
                request = _callbackIdRequest[meta.CallbackId];
                _callbackIdRequest.Remove(meta.CallbackId);
            }

            base.RaiseCallbackAsync(meta, notifyUser,

            repeatCallbackTime =>
            {
                lock (_callbackIdRequest)
                {
                    SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                    {
                        UNTimeIntervalNotificationTrigger callbackNotificationTrigger = UNTimeIntervalNotificationTrigger.CreateTrigger((DateTime.Now - repeatCallbackTime).TotalDays, false);
                        UNNotificationRequest callbackNotificationRequest = UNNotificationRequest.FromIdentifier(meta.CallbackId, request.Content, callbackNotificationTrigger);
                        AddCallbackNotificationRequest(callbackNotificationRequest, meta.IsRepeating, true);
                    });
                }
            },

            letDeviceSleepCallback, finishedCallback);
        }

        public override void UpdateCallbackActivationIdsAsync(string newActivationId)
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                // since all notifications are about to be rescheduled, clear all current notifications.
                UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications();
                UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();

                // see corresponding comment in UILocalNotificationCallbackScheduler
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
                                var repeating    = (request.Content.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEATING_KEY)) as NSNumber).BoolValue;
                                var repeatDelayMS = (request.Content.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEAT_DELAY_KEY)) as NSNumber).Int32Value;
                                var repeatLag    = (request.Content.UserInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEAT_LAG_KEY)) as NSNumber).BoolValue;
                                
                                ((UNMutableNotificationContent)request.Content).UserInfo = GetCallbackInfo(callbackId, repeating, repeatDelayMS, repeatLag, newActivationId);

                                // since we set the UILocalNotification's FireDate when it was constructed, if it's currently in the past it will fire immediately 
                                // when scheduled again with the new activation ID.
                                if (request.Content.UserInfo != null)
                                    AddCallbackNotificationRequest(request, repeating, false);
                            }
                        }
                    }
                }
            });
        }

        protected override void UnscheduleCallbackPlatformSpecific(string callbackId)
        {
            lock (_callbackIdRequest)
            {
                // there are race conditions on this collection, and the key might be removed elsewhere
                UNNotificationRequest request;
                if (_callbackIdRequest.TryGetValue(callbackId, out request))
                {
                    (SensusContext.Current.Notifier as IUNUserNotificationNotifier)?.CancelNotification(callbackId);
                    _callbackIdRequest.Remove(callbackId);
                }
            }
        }
    }
}
