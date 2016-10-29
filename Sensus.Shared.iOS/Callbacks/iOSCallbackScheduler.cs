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
using Sensus.Exceptions;
using Sensus.UI;
using UIKit;
using Xamarin.Forms;

namespace Sensus.iOS.Callbacks
{
    public abstract class iOSCallbackScheduler : CallbackScheduler, IiOSCallbackScheduler
    {
        protected override void ScheduleRepeatingCallback(string callbackId, int initialDelayMS, int repeatDelayMS, bool repeatLag)
        {
            ScheduleCallbackAsync(callbackId, initialDelayMS, true, repeatDelayMS, repeatLag);
        }

        protected override void ScheduleOneTimeCallback(string callbackId, int delayMS)
        {
            ScheduleCallbackAsync(callbackId, delayMS, false, -1, false);
        }

        protected abstract void ScheduleCallbackAsync(string callbackId, int delayMS, bool repeating, int repeatDelayMS, bool repeatLag);

        public abstract void UpdateCallbackNotifications();

        public NSMutableDictionary GetCallbackInfo(string callbackId, bool repeating, int repeatDelayMS, bool repeatLag, DisplayPage displayPage)
        {
            // we've seen cases where the UserInfo dictionary cannot be serialized because one of its values is null. if this happens, the 
            // callback won't be serviced, and things won't return to normal until Sensus is activated by the user and the callbacks are 
            // refreshed. don't create the UserInfo dictionary if we've got null values.
            //
            // see:  https://insights.xamarin.com/app/Sensus-Production/issues/64
            // 
            if (callbackId == null)
                return null;

            List<object> keyValuePairs = new object[]
            {
                Notifier.NOTIFICATION_ID_KEY, callbackId,
                Notifier.DISPLAY_PAGE_KEY, displayPage.ToString(),
                SENSUS_CALLBACK_REPEATING_KEY, repeating,
                SENSUS_CALLBACK_REPEAT_DELAY_KEY, repeatDelayMS,
                SENSUS_CALLBACK_REPEAT_LAG_KEY, repeatLag

            }.ToList();

            return new NSMutableDictionary(new NSDictionary(SENSUS_CALLBACK_KEY, true, keyValuePairs.ToArray()));
        }

        public void ServiceCallbackAsync(NSDictionary callbackInfo)
        {
            // check whether the passed information describes a callback
            NSNumber isCallback = callbackInfo?.ValueForKey(new NSString(SENSUS_CALLBACK_KEY)) as NSNumber;
            if (!(isCallback?.BoolValue ?? false))
                return;

            string callbackId = (callbackInfo.ValueForKey(new NSString(Notifier.NOTIFICATION_ID_KEY)) as NSString).ToString();
            bool repeating = (callbackInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEATING_KEY)) as NSNumber).BoolValue;
            int repeatDelayMS = (callbackInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEAT_DELAY_KEY)) as NSNumber).Int32Value;
            bool repeatLag = (callbackInfo.ValueForKey(new NSString(SENSUS_CALLBACK_REPEAT_LAG_KEY)) as NSNumber).BoolValue;

            // only raise callback if it is still scheduled
            if (!CallbackIsScheduled(callbackId))
                return;

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
            RaiseCallbackAsync(callbackId, repeating, repeatDelayMS, repeatLag, false,

            // don't need to specify how repeats will be scheduled. the class that extends this one will take care of it.
            null,

            // nothing to do if the callback thinks we can sleep. ios does not provide wake-locks like android.
            null,

            // we've completed the raising process, so end background task
            () =>
            {
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
                SensusContext.Current.Notifier.OpenDisplayPage(displayPage);
        }
    }
}