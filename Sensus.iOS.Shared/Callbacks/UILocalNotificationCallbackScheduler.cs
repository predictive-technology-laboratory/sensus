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
using Sensus.Callbacks;
using Sensus.Context;
using UIKit;
using Xamarin.Forms.Platform.iOS;
using System.Threading.Tasks;
using Sensus.Exceptions;
using Sensus.Notifications;
using Sensus.iOS.Notifications.UILocalNotifications;

namespace Sensus.iOS.Callbacks
{
    public class UILocalNotificationCallbackScheduler : iOSCallbackScheduler
    {
        private Dictionary<string, UILocalNotification> _callbackIdNotification;

        public override List<string> CallbackIds
        {
            get
            {
                List<string> callbackIds;

                lock (_callbackIdNotification)
                {
                    callbackIds = _callbackIdNotification.Keys.ToList();
                }

                return callbackIds;
            }
        }

        public UILocalNotificationCallbackScheduler()
        {
            _callbackIdNotification = new Dictionary<string, UILocalNotification>();
        }

        protected override void ScheduleCallbackPlatformSpecific(ScheduledCallback callback)
        {
            // get the callback information. this can be null if we don't have all required information. don't schedule the notification if this happens.
            NSMutableDictionary callbackInfo = GetCallbackInfo(callback);
            if (callbackInfo == null)
            {
                return;
            }

            Action<UILocalNotification> notificationCreated = notification =>
            {
                lock (_callbackIdNotification)
                {
                    _callbackIdNotification.Add(callback.Id, notification);
                }
            };

            UILocalNotificationNotifier notifier = SensusContext.Current.Notifier as UILocalNotificationNotifier;

            if (callback.Silent)
            {
                notifier.IssueSilentNotificationAsync(callback.Id, callback.NextExecution.Value, callbackInfo, notificationCreated);
            }
            else
            {
                notifier.IssueNotificationAsync(callback.Protocol?.Name ?? "Alert", callback.UserNotificationMessage, callback.Id, callback.Protocol, true, callback.DisplayPage, callback.NextExecution.Value, callbackInfo, notificationCreated);
            }
        }

        protected override void ReissueSilentNotification(string id)
        {
            // the following needs to be done on the main thread since we're working with UILocalNotification objects.
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                UILocalNotificationNotifier notifier = SensusContext.Current.Notifier as UILocalNotificationNotifier;

                UILocalNotification notification;

                lock (_callbackIdNotification)
                {
                    notification = _callbackIdNotification[id];
                }

                notifier.IssueNotificationAsync(notification);
            });
        }

        public override Task RaiseCallbackAsync(ScheduledCallback callback, string invocationId, bool notifyUser, Action scheduleRepeatCallback, Action letDeviceSleepCallback)
        {
            return Task.Run(async () =>
            {
                await base.RaiseCallbackAsync(callback, invocationId, notifyUser,

                    // reissue the callback notification
                    () =>
                    {
                        // need to be on UI thread because we are working with UILocalNotifications
                        SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                        {
                            lock (_callbackIdNotification)
                            {
                                UILocalNotification callbackNotification;
                                _callbackIdNotification.TryGetValue(callback.Id, out callbackNotification);

                                // might have been unscheduled
                                if (callbackNotification != null)
                                {
                                    // set the next execution date
                                    callbackNotification.FireDate = callback.NextExecution.Value.ToUniversalTime().ToNSDate();

                                    // update the user info with the new invocation ID that has been set on the callback
                                    NSMutableDictionary newUserInfo = callbackNotification.UserInfo.MutableCopy() as NSMutableDictionary;
                                    newUserInfo.SetValueForKey(new NSString(callback.InvocationId), new NSString(SENSUS_CALLBACK_INVOCATION_ID_KEY));
                                    callbackNotification.UserInfo = newUserInfo;

                                    // reissue the notification
                                    (SensusContext.Current.Notifier as UILocalNotificationNotifier).IssueNotificationAsync(callbackNotification);
                                }
                            }
                        });
                    },

                    letDeviceSleepCallback);
            });
        }

        protected override void UnscheduleCallbackPlatformSpecific(ScheduledCallback callback)
        {
            lock (_callbackIdNotification)
            {
                // there are race conditions on this collection, and the key might be removed elsewhere
                UILocalNotification notification;
                if (_callbackIdNotification.TryGetValue(callback.Id, out notification))
                {
                    (SensusContext.Current.Notifier as UILocalNotificationNotifier)?.CancelNotification(notification);
                    _callbackIdNotification.Remove(callback.Id);
                }
            }
        }

        public override void CancelSilentNotifications()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                UILocalNotificationNotifier notifier = SensusContext.Current.Notifier as UILocalNotificationNotifier;

                foreach (UILocalNotification scheduledNotification in UIApplication.SharedApplication.ScheduledLocalNotifications)
                {
                    if (TryGetCallback(scheduledNotification.UserInfo)?.Silent ?? false)
                    {
                        notifier.CancelNotification(scheduledNotification);
                    }
                }
            });
        }
    }
}