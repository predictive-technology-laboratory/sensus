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
using Xamarin.Forms.Platform.iOS;

namespace Sensus.iOS.Callbacks.UNUserNotifications
{
    public class UNUserNotificationCallbackScheduler : iOSCallbackScheduler
    {
        private Dictionary<string, UNNotificationRequest> _callbackIdRequest;

        public UNUserNotificationCallbackScheduler()
        {
            _callbackIdRequest = new Dictionary<string, UNNotificationRequest>();
        }

        protected override void ScheduleCallbackAsync(string callbackId, TimeSpan delay, bool repeating, TimeSpan repeatDelay, bool repeatLag)
        {
            // get the callback information. this can be null if we don't have all required information. don't schedule the notification if this happens.
            DisplayPage displayPage = GetCallbackDisplayPage(callbackId);
            NSMutableDictionary callbackInfo = GetCallbackInfo(callbackId, repeating, repeatDelay, repeatLag, displayPage);
            if (callbackInfo == null)
            {
                return;
            }

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
            {
                notifier.IssueSilentNotificationAsync(callbackId, delay, callbackInfo, requestCreated);
            }
            else
            {
                notifier.IssueNotificationAsync("Sensus", userNotificationMessage, callbackId, GetCallbackProtocolId(callbackId), true, displayPage, delay, callbackInfo, requestCreated);
            }
        }

        public override Task UpdateCallbacksAsync()
        {
            return Task.Run(async () =>
            {
                IUNUserNotificationNotifier notifier = SensusContext.Current.Notifier as IUNUserNotificationNotifier;

                // get a list of requests to update. cannot iterate over the requests directly because the raising
                // process is going to temporarily modify the collection.
                List<UNNotificationRequest> requests = null;
                lock (_callbackIdRequest)
                {
                    requests = _callbackIdRequest.Values.ToList();
                }

                foreach (UNNotificationRequest request in requests)
                {
                    TimeSpan timeTillTrigger = TimeSpan.Zero;
                    DateTime? triggerDateTime = (request.Trigger as UNCalendarNotificationTrigger)?.NextTriggerDate?.ToDateTime().ToLocalTime();
                    if (triggerDateTime.HasValue)
                    {
                        timeTillTrigger = triggerDateTime.Value - DateTime.Now;
                    }

                    // service any callback that should have already been serviced or will soon be serviced
                    if (timeTillTrigger.TotalSeconds < 5)
                    {
                        notifier.CancelNotification(request);
                        await ServiceCallbackAsync(request.Content?.UserInfo);
                    }
                    // all other callbacks will have upcoming notification deliveries, except for silent notifications, which were canceled when the 
                    // app was backgrounded. re-issue those silent notifications now.
                    else if (iOSNotifier.IsSilent(request.Content?.UserInfo))
                    {
                        notifier.IssueNotificationAsync(request.Identifier, request.Content, timeTillTrigger, newRequest =>
                        {
                            lock (_callbackIdRequest)
                            {
                                _callbackIdRequest[request.Identifier] = newRequest;
                            }
                        });
                    }
                    else
                    {
                        SensusServiceHelper.Get().Logger.Log("Non-silent callback notification request " + request.Identifier + " has upcoming trigger time of " + triggerDateTime, LoggingLevel.Normal, GetType());
                    }
                }
            });
        }

        public override Task RaiseCallbackAsync(string callbackId, bool repeating, TimeSpan repeatDelay, bool repeatLag, bool notifyUser, Action<DateTime> scheduleRepeatCallback, Action letDeviceSleepCallback)
        {
            return Task.Run(async () =>
            {
                // see corresponding comments in UILocalNotificationCallbackScheduler
                UNNotificationRequest request;
                lock (_callbackIdRequest)
                {
                    _callbackIdRequest.TryGetValue(callbackId, out request);
                    _callbackIdRequest.Remove(callbackId);
                }

                await base.RaiseCallbackAsync(callbackId, repeating, repeatDelay, repeatLag, notifyUser,

                    repeatCallbackTime =>
                    {
                        (SensusContext.Current.Notifier as IUNUserNotificationNotifier).IssueNotificationAsync(request.Identifier, request.Content, repeatCallbackTime - DateTime.Now, newRequest =>
                        {
                            lock (_callbackIdRequest)
                            {
                                _callbackIdRequest.Add(newRequest.Identifier, newRequest);
                            }
                        });
                    },

                    letDeviceSleepCallback
                );
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
                    (SensusContext.Current.Notifier as IUNUserNotificationNotifier)?.CancelNotification(request);
                    _callbackIdRequest.Remove(callbackId);
                }
            }
        }
    }
}