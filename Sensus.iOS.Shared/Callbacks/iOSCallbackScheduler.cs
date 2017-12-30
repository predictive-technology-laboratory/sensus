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
using UIKit;

namespace Sensus.iOS.Callbacks
{
    public abstract class iOSCallbackScheduler : CallbackScheduler, IiOSCallbackScheduler
    {
        protected override void ScheduleRepeatingCallbackPlatformSpecific(string callbackId, TimeSpan initialDelay, TimeSpan repeatDelay, bool repeatLag)
        {
            ScheduleCallbackAsync(callbackId, initialDelay, true, repeatDelay, repeatLag);
        }

        protected override void ScheduleOneTimeCallbackPlatformSpecific(string callbackId, TimeSpan delay)
        {
            ScheduleCallbackAsync(callbackId, delay, false, TimeSpan.Zero, false);
        }

        protected abstract void ScheduleCallbackAsync(string callbackId, TimeSpan delay, bool repeating, TimeSpan repeatDelay, bool repeatLag);

        public abstract Task UpdateCallbacksAsync();

        public NSMutableDictionary GetCallbackInfo(string callbackId, bool repeating, TimeSpan repeatDelay, bool repeatLag, DisplayPage displayPage)
        {
            // we've seen cases where the UserInfo dictionary cannot be serialized because one of its values is null. if this happens, the 
            // callback won't be serviced, and things won't return to normal until Sensus is activated by the user and the callbacks are 
            // refreshed. don't create the UserInfo dictionary if we've got null values.
            //
            // see:  https://insights.xamarin.com/app/Sensus-Production/issues/64
            // 
            if (callbackId == null)
            {
                return null;
            }

            List<object> keyValuePairs = new object[]
            {
                iOSNotifier.NOTIFICATION_ID_KEY, callbackId,
                Notifier.DISPLAY_PAGE_KEY, displayPage.ToString(),
                SENSUS_CALLBACK_REPEATING_KEY, repeating,
                SENSUS_CALLBACK_REPEAT_DELAY_KEY, repeatDelay.Ticks.ToString(),
                SENSUS_CALLBACK_REPEAT_LAG_KEY, repeatLag

            }.ToList();

            return new NSMutableDictionary(new NSDictionary(SENSUS_CALLBACK_KEY, true, keyValuePairs.ToArray()));
        }

        public Task ServiceCallbackAsync(NSDictionary callbackInfo)
        {
            return Task.Run(async () =>
            {
                // check whether the passed information describes a callback
                NSNumber isCallback = callbackInfo?.ValueForKey(new NSString(SENSUS_CALLBACK_KEY)) as NSNumber;
                if (!(isCallback?.BoolValue ?? false))
                {
                    return;
                }

                // not sure why the following would be null, but we've seen NRE in insights and these are the likely suspects.
                string callbackId = (callbackInfo.ValueForKey(new NSString(iOSNotifier.NOTIFICATION_ID_KEY)) as NSString)?.ToString();
                bool repeating = (callbackInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEATING_KEY)) as NSNumber)?.BoolValue ?? false;

                TimeSpan repeatDelay = TimeSpan.Zero;
                if (repeating)
                {
                    repeatDelay = TimeSpan.FromTicks(long.Parse(callbackInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEAT_DELAY_KEY)) as NSString));
                }

                bool repeatLag = (callbackInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEAT_LAG_KEY)) as NSNumber)?.BoolValue ?? false;

                // only raise callback if it is still scheduled
                if (!CallbackIsScheduled(callbackId))
                {
                    return;
                }

                SensusServiceHelper.Get().Logger.Log("Servicing callback " + callbackId, LoggingLevel.Normal, GetType());

                // start background task
                nint callbackTaskId = -1;
                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                {
                    callbackTaskId = UIApplication.SharedApplication.BeginBackgroundTask(() =>
                    {
                        // if we're out of time running in the background, cancel the callback.
                        CancelRaisedCallback(callbackId);
                    });
                });

                // raise callback but don't notify user since we would have already done so when the notification was delivered to the notification tray.
                // we don't need to specify how repeats will be scheduled, since the class that extends this one will take care of it. furthermore, there's 
                // nothing to do if the callback thinks we can sleep, since ios does not provide wake-locks like android.
                await RaiseCallbackAsync(callbackId, repeating, repeatDelay, repeatLag, false, null, null);

                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                {
                    UIApplication.SharedApplication.EndBackgroundTask(callbackTaskId);
                });
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
    }
}