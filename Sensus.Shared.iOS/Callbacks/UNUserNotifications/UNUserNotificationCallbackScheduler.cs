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

using Sensus.Shared.Context;
using Sensus.Shared.Callbacks;
using Sensus.Shared.Exceptions;

using UserNotifications;

namespace Sensus.Shared.iOS.Callbacks.UNUserNotifications
{
    public class UNUserNotificationCallbackScheduler : iOSCallbackScheduler
    {
        private readonly Dictionary<string, UNNotificationRequest> _callbackIdRequest;

        public UNUserNotificationCallbackScheduler()
        {
            _callbackIdRequest = new Dictionary<string, UNNotificationRequest>();
        }

        protected override void ScheduleCallbackAsync(ICallbackData meta)
        {
            string userNotificationMessage = GetCallbackUserNotificationMessage(meta.CallbackId);

            if (meta.CallbackId == null || SensusContext.Current.ActivationId == null) return;

            var callbackNotificationContent = new UNMutableNotificationContent { Title = "Sensus" };

            new iOSCallbackData(callbackNotificationContent)
            {
                CallbackId   = meta.CallbackId,
                IsRepeating  = meta.IsRepeating,
                RepeatDelay  = meta.RepeatDelay,
                LagAllowed   = meta.LagAllowed,
                ActivationId = SensusContext.Current.ActivationId
            };

            if (userNotificationMessage != null)
            {
                callbackNotificationContent.Body = userNotificationMessage;
                callbackNotificationContent.Sound = UNNotificationSound.Default;
            }

            var callbackNotificationTrigger = UNTimeIntervalNotificationTrigger.CreateTrigger(meta.RepeatDelay.TotalSeconds, false);
            var callbackNotificationRequest = UNNotificationRequest.FromIdentifier(meta.CallbackId, callbackNotificationContent, callbackNotificationTrigger);

            AddCallbackNotificationRequest(callbackNotificationRequest, meta.IsRepeating, true);
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
                            SensusServiceHelper.Get().Logger.Log($"Callback {request.Identifier} scheduled for {((UNTimeIntervalNotificationTrigger)request.Trigger).NextTriggerDate} (" + (repeating ? "repeating" : "one-time") + "). " + _callbackIdRequest.Count + " total callbacks in scheduler.", LoggingLevel.Normal, GetType());
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

        public override void RaiseCallbackAsync(ICallbackData meta, bool notifyUser, Action<DateTime> scheduleRepeatCallback, Action letDeviceSleepCallback, Action finishedCallback)
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
                        var callbackNotificationTrigger = UNTimeIntervalNotificationTrigger.CreateTrigger((DateTime.Now - repeatCallbackTime).TotalDays, false);
                        var callbackNotificationRequest = UNNotificationRequest.FromIdentifier(meta.CallbackId, request.Content, callbackNotificationTrigger);
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
                        var request = _callbackIdRequest[callbackId];

                        if (request.Content.UserInfo != null)
                        {
                            var meta = new iOSCallbackData(request.Content.UserInfo);

                            if (meta.ActivationId != newActivationId)
                            {
                                meta.ActivationId = newActivationId;

                                // since we set the UILocalNotification's FireDate when it was constructed
                                // if it's currently in the past it will fire immediately when scheduled again with the new activation ID.
                                if (request.Content.UserInfo != null) AddCallbackNotificationRequest(request, meta.IsRepeating, false);
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
