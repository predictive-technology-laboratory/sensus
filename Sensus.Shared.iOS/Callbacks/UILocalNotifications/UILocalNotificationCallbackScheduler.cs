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

namespace Sensus.iOS.Callbacks.UILocalNotifications
{
    public class UILocalNotificationCallbackScheduler : iOSCallbackScheduler
    {
        private Dictionary<string, UILocalNotification> _callbackIdNotification;

        public UILocalNotificationCallbackScheduler()
        {
            _callbackIdNotification = new Dictionary<string, UILocalNotification>();
        }

        protected override void ScheduleCallbackAsync(string callbackId, int delayMS, bool repeating, int repeatDelayMS, bool repeatLag)
        {
            // get the callback information. this can be null if we don't have all required information. don't schedule the notification if this happens.
            DisplayPage displayPage = GetCallbackDisplayPage(callbackId);
            NSMutableDictionary callbackInfo = GetCallbackInfo(callbackId, repeating, repeatDelayMS, repeatLag, displayPage);
            if (callbackInfo == null)
                return;

            Action<UILocalNotification> notificationCreated = notification =>
            {
                lock (_callbackIdNotification)
                {
                    _callbackIdNotification.Add(callbackId, notification);
                }
            };

            IUILocalNotificationNotifier notifier = SensusContext.Current.Notifier as IUILocalNotificationNotifier;

            string userNotificationMessage = GetCallbackUserNotificationMessage(callbackId);
            if (userNotificationMessage == null)
                notifier.IssueSilentNotificationAsync(callbackId, delayMS, callbackInfo, notificationCreated);
            else
            {
                notifier.IssueNotificationAsync("Sensus", userNotificationMessage, callbackId, GetCallbackProtocolId(callbackId), true, displayPage, delayMS, callbackInfo, notificationCreated);
            }
        }

        /// <summary>
        /// Updates the callbacks by running any that should have already been serviced or will be serviced in the near future.
        /// Also reissues all silent notifications, which would have been canceled when the app went into the background.
        /// </summary>
        /// <returns>The callbacks async.</returns>
        public override Task UpdateCallbacksAsync()
        {
            return Task.Run(() =>
            {
                IUILocalNotificationNotifier notifier = SensusContext.Current.Notifier as IUILocalNotificationNotifier;

                // get a list of notifications to update. cannot iterate over the notifications directly because the raising
                // process is going to temporarily modify the collection.
                List<UILocalNotification> notifications = null;
                lock (_callbackIdNotification)
                {
                    notifications = _callbackIdNotification.Values.ToList();
                }

                foreach (UILocalNotification notification in notifications)
                {
                    // the following needs to be done on the main thread since we're working with UILocalNotification objects.
                    SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                    {
                        double msTillTrigger = 0;
                        DateTime? triggerDateTime = notification.FireDate?.ToDateTime().ToLocalTime();
                        if (triggerDateTime.HasValue)
                            msTillTrigger = (triggerDateTime.Value - DateTime.Now).TotalMilliseconds;

                        // service any callback that should have already been serviced or will soon be serviced
                        if (msTillTrigger < 5000)
                        {
                            notifier.CancelNotification(notification);
                            ServiceCallbackAsync(notification.UserInfo);
                        }
                        // all other callbacks will have upcoming notification deliveries, except for silent notifications, which were canceled when the 
                        // app was backgrounded. re-issue those silent notifications now.
                        else if (iOSNotifier.IsSilent(notification.UserInfo))
                        {
                            notifier.IssueNotificationAsync(notification);
                        }
                    });
                }
            });
        }

        public override Task RaiseCallbackAsync(string callbackId, bool repeating, int repeatDelayMS, bool repeatLag, bool notifyUser, Action<DateTime> scheduleRepeatCallback, Action letDeviceSleepCallback)
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
                    _callbackIdNotification.TryGetValue(callbackId, out callbackNotification);
                    _callbackIdNotification.Remove(callbackId);
                }

                await base.RaiseCallbackAsync(callbackId, repeating, repeatDelayMS, repeatLag, notifyUser,

                    repeatCallbackTime =>
                    {
                        // add to the platform-specific notification collection, so that the notification is updated and reissued if/when the app is reactivated
                        lock (_callbackIdNotification)
                        {
                            _callbackIdNotification.Add(callbackId, callbackNotification);
                        }

                        SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                        {
                            callbackNotification.FireDate = repeatCallbackTime.ToUniversalTime().ToNSDate();
                            (SensusContext.Current.Notifier as IUILocalNotificationNotifier).IssueNotificationAsync(callbackNotification);
                        });
                    },

                    letDeviceSleepCallback
                );
            });
        }

        protected override void UnscheduleCallbackPlatformSpecific(string callbackId)
        {
            lock (_callbackIdNotification)
            {
                // there are race conditions on this collection, and the key might be removed elsewhere
                UILocalNotification notification;
                if (_callbackIdNotification.TryGetValue(callbackId, out notification))
                {
                    (SensusContext.Current.Notifier as IUILocalNotificationNotifier)?.CancelNotification(notification);
                    _callbackIdNotification.Remove(callbackId);
                }
            }
        }
    }
}