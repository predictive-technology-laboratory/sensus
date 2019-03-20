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

        protected override async Task RequestLocalInvocationAsync(ScheduledCallback callback)
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
                    _callbackIdRequest[callback.Id] = request;
                }
            };

            UNUserNotificationNotifier notifier = SensusContext.Current.Notifier as UNUserNotificationNotifier;

            if (callback.Silent)
            {
                await notifier.IssueSilentNotificationAsync(callback.Id, callback.NextExecution.Value, callbackInfo, requestCreated);
            }
            else
            {
                await notifier.IssueNotificationAsync(callback.Protocol?.Name ?? "Alert", callback.UserNotificationMessage, callback.Id, true, callback.Protocol, null, callback.NotificationUserResponseAction, callback.NotificationUserResponseMessage, callback.NextExecution.Value, callbackInfo, requestCreated);
            }
        }

        protected override async Task ReissueSilentNotificationAsync(string id)
        {
            // the following needs to be done on the main thread
            await SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
            {
                UNUserNotificationNotifier notifier = SensusContext.Current.Notifier as UNUserNotificationNotifier;

                UNNotificationRequest request;

                lock (_callbackIdRequest)
                {
                    request = _callbackIdRequest[id];
                }

                await notifier.IssueNotificationAsync(request);
            });
        }

        protected override void CancelLocalInvocation(ScheduledCallback callback)
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