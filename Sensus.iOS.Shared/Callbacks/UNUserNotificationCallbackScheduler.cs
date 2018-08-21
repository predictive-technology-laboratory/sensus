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
using System.Threading.Tasks;
using Foundation;
using Sensus.Callbacks;
using Sensus.Context;
using UserNotifications;
using Sensus.iOS.Notifications.UNUserNotifications;

namespace Sensus.iOS.Callbacks
{
    public class UNUserNotificationCallbackScheduler : iOSCallbackScheduler
    {
        private Dictionary<string, UNNotificationRequest> _callbackIdRequest;

        public override List<string> CallbackIds
        {
            get
            {
                List<string> callbackIds;

                lock (_callbackIdRequest)
                {
                    callbackIds = _callbackIdRequest.Keys.ToList();
                }

                return callbackIds;
            }
        }

        public UNUserNotificationCallbackScheduler()
        {
            _callbackIdRequest = new Dictionary<string, UNNotificationRequest>();
        }

        protected override void ScheduleCallbackPlatformSpecific(ScheduledCallback callback)
        {
            // get the callback information. this can be null if we don't have all required information. don't schedule the notification if this happens.
            NSMutableDictionary callbackInfo = GetCallbackInfo(callback);
            if (callbackInfo == null)
            {
                return;
            }

            Action<UNNotificationRequest> requestCreated = request =>
            {
                lock (_callbackIdRequest)
                {
                    _callbackIdRequest.Add(callback.Id, request);
                }
            };

            UNUserNotificationNotifier notifier = SensusContext.Current.Notifier as UNUserNotificationNotifier;

            if (callback.Silent)
            {
                notifier.IssueSilentNotificationAsync(callback.Id, callback.NextExecution.Value, callbackInfo, requestCreated);
            }
            else
            {
                notifier.IssueNotificationAsync(callback.Protocol?.Name ?? "Alert", callback.UserNotificationMessage, callback.Id, callback.Protocol, true, callback.DisplayPage, callback.NextExecution.Value, callbackInfo, requestCreated);
            }
        }

        protected override void ReissueSilentNotification(string id)
        {
            // the following needs to be done on the main thread since we're working with UILocalNotification objects.
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                UNUserNotificationNotifier notifier = SensusContext.Current.Notifier as UNUserNotificationNotifier;

                UNNotificationRequest request;

                lock (_callbackIdRequest)
                {
                    request = _callbackIdRequest[id];
                }

                notifier.IssueNotificationAsync(request);
            });
        }

        public override Task RaiseCallbackAsync(ScheduledCallback callback, string invocationId, bool notifyUser, Action scheduleRepeatCallback, Action letDeviceSleepCallback)
        {
            return Task.Run(async () =>
            {
                await base.RaiseCallbackAsync(callback, invocationId, notifyUser,

                    () =>
                    {
                        // reissue the callback notification request
                        lock (_callbackIdRequest)
                        {
                            UNNotificationRequest request;
                            _callbackIdRequest.TryGetValue(callback.Id, out request);

                            // might have been unscheduled
                            if (request != null)
                            {
                                // update the request's user info with the new invocation ID
                                NSMutableDictionary newUserInfo = request.Content.UserInfo.MutableCopy() as NSMutableDictionary;
                                newUserInfo.SetValueForKey(new NSString(callback.InvocationId), new NSString(SENSUS_CALLBACK_INVOCATION_ID_KEY));
                                UNMutableNotificationContent newContent = request.Content.MutableCopy() as UNMutableNotificationContent;
                                newContent.UserInfo = newUserInfo;

                                // reissue the notification request using the next execution date on the callback. the following call will not return until
                                // the request has been created, ensuring that the request has been updated in _callbackIdRequest before the next caller 
                                // obtains the current lock.
                                (SensusContext.Current.Notifier as UNUserNotificationNotifier).IssueNotificationAsync(request.Identifier, newContent, callback.NextExecution.Value, newRequest =>
                                {
                                    lock (_callbackIdRequest)
                                    {
                                        _callbackIdRequest[newRequest.Identifier] = newRequest;
                                    }
                                });
                            }
                        }
                    },

                    letDeviceSleepCallback
                );
            });
        }

        protected override void UnscheduleCallbackPlatformSpecific(ScheduledCallback callback)
        {
            lock (_callbackIdRequest)
            {
                // there are race conditions on this collection, and the key might be removed elsewhere
                UNNotificationRequest request;
                if (_callbackIdRequest.TryGetValue(callback.Id, out request))
                {
                    (SensusContext.Current.Notifier as UNUserNotificationNotifier)?.CancelNotification(request);
                    _callbackIdRequest.Remove(callback.Id);
                }
            }
        }

        public override void CancelSilentNotifications()
        {
            UNUserNotificationCenter.Current.GetPendingNotificationRequests(requests =>
            {
                UNUserNotificationNotifier notifier = SensusContext.Current.Notifier as UNUserNotificationNotifier;

                foreach (UNNotificationRequest request in requests)
                {
                    if (TryGetCallback(request.Content?.UserInfo)?.Silent ?? false)
                    {
                        notifier.CancelNotification(request);
                    }
                }
            });
        }
    }
}