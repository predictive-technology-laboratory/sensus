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

        protected override Task ScheduleCallbackPlatformSpecificAsync(ScheduledCallback callback)
        {
            // get the callback information. this can be null if we don't have all required information. don't schedule the notification if this happens.
            NSMutableDictionary callbackInfo = GetCallbackInfo(callback);
            if (callbackInfo == null)
            {
                return Task.CompletedTask;
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
                notifier.IssueSilentNotification(callback.Id, callback.NextExecution.Value, callbackInfo, notificationCreated);
            }
            else
            {
                notifier.IssueNotification(callback.Protocol?.Name ?? "Alert", callback.UserNotificationMessage, callback.Id, callback.Protocol, true, callback.DisplayPage, callback.NextExecution.Value, callbackInfo, notificationCreated);
            }

            return Task.CompletedTask;
        }

        protected override Task ReissueSilentNotificationAsync(string id)
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

                notifier.IssueNotification(notification);
            });

            return Task.CompletedTask;
        }

        public override async Task RaiseCallbackAsync(ScheduledCallback callback, string invocationId, bool notifyUser, Func<Task> scheduleRepeatCallbackAsync, Action letDeviceSleepCallback)
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
                                (SensusContext.Current.Notifier as UILocalNotificationNotifier).IssueNotification(callbackNotification);
                            }
                        }
                    });

                    return Task.CompletedTask;
                },

                letDeviceSleepCallback);
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
