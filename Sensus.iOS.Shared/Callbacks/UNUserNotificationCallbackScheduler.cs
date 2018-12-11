//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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

        protected override async Task ScheduleCallbackPlatformSpecificAsync(ScheduledCallback callback)
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
                await notifier.IssueSilentNotificationAsync(callback.Id, callback.NextExecution.Value, callbackInfo, requestCreated);
            }
            else
            {
                await notifier.IssueNotificationAsync(callback.Protocol?.Name ?? "Alert", callback.UserNotificationMessage, callback.Id, callback.Protocol, true, callback.DisplayPage, callback.NextExecution.Value, callbackInfo, requestCreated);
            }
        }

        protected override async Task ReissueSilentNotificationAsync(string id)
        {
            // the following needs to be done on the main thread since we're working with UILocalNotification objects.
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

        public override async Task RaiseCallbackAsync(ScheduledCallback callback, string invocationId, bool notifyUser, Func<Task> scheduleRepeatCallbackAsync, Action letDeviceSleepCallback)
        {
            await base.RaiseCallbackAsync(callback, invocationId, notifyUser,

                async () =>
                {
                    // reissue the callback notification request

                    UNNotificationRequest request;
                    lock (_callbackIdRequest)
                    {
                        _callbackIdRequest.TryGetValue(callback.Id, out request);
                    }

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
                        await (SensusContext.Current.Notifier as UNUserNotificationNotifier).IssueNotificationAsync(request.Identifier, newContent, callback.NextExecution.Value, newRequest =>
                        {
                            lock (_callbackIdRequest)
                            {
                                _callbackIdRequest[newRequest.Identifier] = newRequest;
                            }
                        });
                    }
                },

                letDeviceSleepCallback
            );
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
