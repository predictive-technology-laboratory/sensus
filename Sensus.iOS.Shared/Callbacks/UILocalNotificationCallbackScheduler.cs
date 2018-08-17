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

            IUILocalNotificationNotifier notifier = SensusContext.Current.Notifier as IUILocalNotificationNotifier;

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
                // remove from platform-specific notification collection before raising the callback. the purpose of the platform-specific notification collection 
                // is to hold the notifications between successive activations of the app. when the app is reactivated, notifications from this collection are 
                // updated with the new activation id and they are rescheduled. if, in raising the callback associated with the current notification, the app is 
                // reactivated (e.g., by a call to the facebook probe login manager), then the current notification will be reissued when updated via app reactivation 
                // (which will occur, e.g., when the facebook login manager returns control to the app). this can lead to duplicate notifications for the same callback, 
                // or infinite cycles of app reactivation if the notification raises a callback that causes it to be reissued (e.g., in the case of facebook login).
                UILocalNotification callbackNotification;
                lock (_callbackIdNotification)
                {
                    _callbackIdNotification.TryGetValue(callback.Id, out callbackNotification);
                    _callbackIdNotification.Remove(callback.Id);
                }

                await base.RaiseCallbackAsync(callback, invocationId, notifyUser,

                    // action to schedule the next callback for a repeating callback
                    () =>
                    {
                        // add to the platform-specific notification collection, so that the notification is updated and reissued if/when the app is reactivated
                        lock (_callbackIdNotification)
                        {
                            _callbackIdNotification.Add(callback.Id, callbackNotification);
                        }

                        SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                        {
                            // set the next execution date
                            callbackNotification.FireDate = callback.NextExecution.Value.ToUniversalTime().ToNSDate();

                            // update the user info with the new invocation ID
                            (callbackNotification.UserInfo as NSMutableDictionary).SetValueForKey(new NSString(callback.InvocationId), new NSString(SENSUS_CALLBACK_INVOCATION_ID_KEY));

                            // reissue the notification
                            (SensusContext.Current.Notifier as IUILocalNotificationNotifier).IssueNotificationAsync(callbackNotification);
                        });
                    },

                    letDeviceSleepCallback
                );
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
                    (SensusContext.Current.Notifier as IUILocalNotificationNotifier)?.CancelNotification(notification);
                    _callbackIdNotification.Remove(callback.Id);
                }
            }
        }

        public override void CancelSilentNotifications()
        {
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                IUILocalNotificationNotifier notifier = SensusContext.Current.Notifier as IUILocalNotificationNotifier;

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