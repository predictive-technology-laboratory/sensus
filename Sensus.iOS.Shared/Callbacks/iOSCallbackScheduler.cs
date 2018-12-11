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
using UIKit;
using Sensus.iOS.Notifications;
using Sensus.Exceptions;
using Sensus.Notifications;

namespace Sensus.iOS.Callbacks
{
    public abstract class iOSCallbackScheduler : CallbackScheduler
    {
        /// <summary>
        /// The callback notification horizon threshold. When using notifications to schedule the timing of
        /// callbacks, we must decide when the delay of a callback execution necessitates a notification 
        /// rather than executing immediately. This is in part a practical question, in that executing a 
        /// callback immediately rather than using a notification can improve performance -- imagine the
        /// app coming to the foreground and executing the callback right away versus deferring it to a 
        /// future notification. This is also an iOS API consideration, as the iOS system will not schedule 
        /// a notification if its fire date is in the past by the time it gets around to doing the scheduling.
        /// </summary>
        public static readonly TimeSpan CALLBACK_NOTIFICATION_HORIZON_THRESHOLD = TimeSpan.FromSeconds(5);

        public abstract List<string> CallbackIds { get; }

        /// <summary>
        /// Updates the callbacks by running any that should have already been serviced or will be serviced in the near future.
        /// Also reissues all silent notifications, which would have been canceled when the app went into the background.
        /// </summary>
        /// <returns>Async task.</returns>
        public async Task UpdateCallbacksAsync()
        {
            foreach (string callbackId in CallbackIds)
            {
                ScheduledCallback callback = TryGetCallback(callbackId);

                if (callback == null)
                {
                    continue;
                }

                // service any callback that should have already been serviced or will soon be serviced
                if (callback.NextExecution.Value < DateTime.Now + CALLBACK_NOTIFICATION_HORIZON_THRESHOLD)
                {
                    iOSNotifier notifier = SensusContext.Current.Notifier as iOSNotifier;
                    notifier.CancelNotification(callback.Id);
                    await ServiceCallbackAsync(callback, callback.InvocationId);
                }
                // all silent notifications (e.g., those for health tests) were cancelled when the app entered background. reissue them now.
                else if (callback.Silent)
                {
                    await ReissueSilentNotificationAsync(callback.Id);
                }
                else
                {
                    SensusServiceHelper.Get().Logger.Log("Non-silent callback notification " + callback.Id + " has upcoming trigger time of " + callback.NextExecution, LoggingLevel.Normal, GetType());
                }
            }
        }

        protected abstract Task ReissueSilentNotificationAsync(string id);

        public NSMutableDictionary GetCallbackInfo(ScheduledCallback callback)
        {
            // we've seen cases where the UserInfo dictionary cannot be serialized because one of its values is null. if this happens, the 
            // callback won't be serviced, and things won't return to normal until Sensus is activated by the user and the callbacks are 
            // refreshed. don't create the UserInfo dictionary if we've got null values.
            if (callback.Id == null)
            {
                SensusException.Report("Failed to get callback information bundle due to null callback ID.");
                return null;
            }

            return new NSMutableDictionary(new NSDictionary(SENSUS_CALLBACK_KEY, true,
                                                            iOSNotifier.NOTIFICATION_ID_KEY, callback.Id,
                                                            SENSUS_CALLBACK_INVOCATION_ID_KEY, callback.InvocationId));
        }

        public ScheduledCallback TryGetCallback(NSDictionary callbackInfo)
        {
            if (IsCallback(callbackInfo))
            {
                return TryGetCallback((callbackInfo.ValueForKey(new NSString(iOSNotifier.NOTIFICATION_ID_KEY)) as NSString)?.ToString());
            }
            else
            {
                return null;
            }
        }

        public bool IsCallback(NSDictionary callbackInfo)
        {
            NSNumber isCallback = callbackInfo?.ValueForKey(new NSString(SENSUS_CALLBACK_KEY)) as NSNumber;
            return isCallback?.BoolValue ?? false;
        }

        public async Task ServiceCallbackAsync(NSDictionary callbackInfo)
        {
            ScheduledCallback callback = TryGetCallback(callbackInfo);
            string invocationId = callbackInfo?.ValueForKey(new NSString(SENSUS_CALLBACK_INVOCATION_ID_KEY)) as NSString;
            await ServiceCallbackAsync(callback, invocationId);
        }

        public override async Task ServiceCallbackAsync(ScheduledCallback callback, string invocationId)
        {
            if (callback == null)
            {
                SensusServiceHelper.Get().Logger.Log("Attempted to service null callback.", LoggingLevel.Normal, GetType());
                return;
            }

            SensusServiceHelper.Get().Logger.Log("Servicing callback " + callback.Id + ".", LoggingLevel.Normal, GetType());

            // start background task for servicing callback
            nint callbackTaskId = -1;
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                SensusServiceHelper.Get().Logger.Log("Starting background task for callback.", LoggingLevel.Normal, GetType());

                callbackTaskId = UIApplication.SharedApplication.BeginBackgroundTask(() =>
                {
                    // if we're out of time running in the background, cancel the callback.
                    CancelRaisedCallback(callback);
                });
            });

            // raise callback but don't notify user since we would have already done so when the notification was delivered to the notification tray.
            // we don't need to specify how repeats will be scheduled, since the class that extends this one will take care of it. furthermore, there's 
            // nothing to do if the callback thinks we can sleep, since ios does not provide wake-locks like android.
            await RaiseCallbackAsync(callback, invocationId, false, null, null);

            // end the background task
            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                SensusServiceHelper.Get().Logger.Log("Ending background task for callback.", LoggingLevel.Normal, GetType());

                UIApplication.SharedApplication.EndBackgroundTask(callbackTaskId);
            });
        }

        public void OpenDisplayPage(NSDictionary notificationInfo)
        {
            DisplayPage displayPage;
            if (Enum.TryParse(notificationInfo?.ValueForKey(new NSString(Notifier.DISPLAY_PAGE_KEY)) as NSString, out displayPage))
            {
                SensusContext.Current.Notifier.OpenDisplayPage(displayPage);
            }
        }

        /// <summary>
        /// Cancels the silent notifications (e.g., those for health test) when the app is going into the background.
        /// </summary>
        public abstract void CancelSilentNotifications();
    }
}
