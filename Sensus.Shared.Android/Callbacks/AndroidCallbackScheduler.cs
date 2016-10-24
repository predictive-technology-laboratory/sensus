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
using Android.App;
using Android.Content;
using Android.OS;
using Sensus.Shared.Callbacks;
using Xamarin.Forms.Platform.Android;

namespace Sensus.Shared.Android.Callbacks
{
    public class AndroidCallbackScheduler<MainActivityT> : CallbackScheduler where MainActivityT : FormsApplicationActivity
    {
        private AndroidSensusService<MainActivityT> _service;
        private Dictionary<string, PendingIntent> _callbackIdPendingIntent;

        public AndroidCallbackScheduler(AndroidSensusService<MainActivityT> service)
        {
            _service = service;
            _callbackIdPendingIntent = new Dictionary<string, PendingIntent>();
        }

        protected override void ScheduleOneTimeCallback(string callbackId, int delayMS)
        {
            DateTime callbackTime = DateTime.Now.AddMilliseconds(delayMS);
            SensusServiceHelper.Get().Logger.Log("Callback " + callbackId + " scheduled for " + callbackTime + " (one-time).", LoggingLevel.Normal, GetType());
            ScheduleCallbackAlarm(CreateCallbackPendingIntent(callbackId, false, 0, false), callbackId, callbackTime);
        }

        protected override void ScheduleRepeatingCallback(string callbackId, int initialDelayMS, int repeatDelayMS, bool repeatLag)
        {
            DateTime callbackTime = DateTime.Now.AddMilliseconds(initialDelayMS);
            SensusServiceHelper.Get().Logger.Log("Callback " + callbackId + " scheduled for " + callbackTime + " (repeating).", LoggingLevel.Normal, GetType());
            ScheduleCallbackAlarm(CreateCallbackPendingIntent(callbackId, true, repeatDelayMS, repeatLag), callbackId, callbackTime);
        }

        private PendingIntent CreateCallbackPendingIntent(string callbackId, bool repeating, int repeatDelayMS, bool repeatLag)
        {
            DisplayPage displayPage = GetCallbackDisplayPage(callbackId);

            Intent callbackIntent = new Intent(_service, typeof(AndroidSensusService<MainActivityT>));
            callbackIntent.PutExtra(Notifier.NOTIFICATION_ID_KEY, callbackId);
            callbackIntent.PutExtra(Notifier.DISPLAY_PAGE_KEY, displayPage.ToString());
            callbackIntent.PutExtra(SENSUS_CALLBACK_KEY, true);
            callbackIntent.PutExtra(SENSUS_CALLBACK_REPEATING_KEY, repeating);
            callbackIntent.PutExtra(SENSUS_CALLBACK_REPEAT_DELAY_KEY, repeatDelayMS);
            callbackIntent.PutExtra(SENSUS_CALLBACK_REPEAT_LAG_KEY, repeatLag);

            return CreateCallbackPendingIntent(callbackIntent);
        }

        public void ServiceCallback(Intent intent)
        {
            SensusServiceHelper serviceHelper = SensusServiceHelper.Get();

            string callbackId = intent.GetStringExtra(Notifier.NOTIFICATION_ID_KEY);

            // if the user removes the main activity from the switcher, the service's process will be killed and restarted without notice, and 
            // we'll have no opportunity to unschedule repeating callbacks. when the service is restarted we'll reinitialize the service
            // helper, restart the repeating callbacks, and we'll then have duplicate repeating callbacks. handle the invalid callbacks below.
            // if the callback is scheduled, it's fine. if it's not, then unschedule it.
            if (CallbackIsScheduled(callbackId))
            {
                bool repeating = intent.GetBooleanExtra(SENSUS_CALLBACK_REPEATING_KEY, false);
                int repeatDelayMS = intent.GetIntExtra(SENSUS_CALLBACK_REPEAT_DELAY_KEY, -1);
                bool repeatLag = intent.GetBooleanExtra(SENSUS_CALLBACK_REPEAT_LAG_KEY, false);
                bool wakeLockReleased = false;

                // raise callback and notify the user if there is a message. we wouldn't have presented the user with the message yet.
                RaiseCallbackAsync(callbackId, repeating, repeatDelayMS, repeatLag, true,

                    // schedule a new callback at the given time.
                    repeatCallbackTime =>
                    {
                        ScheduleCallbackAlarm(CreateCallbackPendingIntent(intent), callbackId, repeatCallbackTime);
                    },

                    // if the callback indicates that it's okay for the device to sleep, release the wake lock now.
                    () =>
                    {
                        wakeLockReleased = true;
                        serviceHelper.LetDeviceSleep();
                        serviceHelper.Logger.Log("Wake lock released preemptively for scheduled callback action.", LoggingLevel.Normal, GetType());
                    },

                    // release wake lock now if we didn't while the callback action was executing.
                    () =>
                    {
                        if (!wakeLockReleased)
                        {
                            serviceHelper.LetDeviceSleep();
                            serviceHelper.Logger.Log("Wake lock released after scheduled callback action completed.", LoggingLevel.Normal, GetType());
                        }
                    });
            }
            else
            {
                UnscheduleCallback(callbackId);
                serviceHelper.LetDeviceSleep();
            }
        }

        protected override void UnscheduleCallbackPlatformSpecific(string callbackId)
        {
            lock (_callbackIdPendingIntent)
            {
                PendingIntent callbackPendingIntent;
                if (_callbackIdPendingIntent.TryGetValue(callbackId, out callbackPendingIntent))
                {
                    AlarmManager alarmManager = _service.GetSystemService(global::Android.Content.Context.AlarmService) as AlarmManager;
                    alarmManager.Cancel(callbackPendingIntent);
                    _callbackIdPendingIntent.Remove(callbackId);
                }
            }
        }

        public PendingIntent CreateCallbackPendingIntent(Intent callbackIntent)
        {
            // upon hash collisions for the request code, the previous intent will simply be canceled.
            return PendingIntent.GetService(_service, callbackIntent.GetStringExtra(Notifier.NOTIFICATION_ID_KEY).GetHashCode(), callbackIntent, PendingIntentFlags.CancelCurrent);
        }

        public void ScheduleCallbackAlarm(PendingIntent callbackPendingIntent, string callbackId, DateTime callbackTime)
        {
            lock (_callbackIdPendingIntent)
            {
                // update pending intent associated with the callback id. we'll need the updated pending intent if/when
                // we which to unschedule the alarm.
                _callbackIdPendingIntent[callbackId] = callbackPendingIntent;

                AlarmManager alarmManager = _service.GetSystemService(global::Android.Content.Context.AlarmService) as AlarmManager;

                long delayMS = (long)(callbackTime - DateTime.Now).TotalMilliseconds;
                long callbackTimeMS = Java.Lang.JavaSystem.CurrentTimeMillis() + delayMS;

                // https://github.com/predictive-technology-laboratory/sensus/wiki/Backwards-Compatibility
#if __ANDROID_23__
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                    alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, callbackTimeMS, callbackPendingIntent);  // API level 23 added "while idle" option, making things even tighter.
                else if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
                    alarmManager.SetExact(AlarmType.RtcWakeup, callbackTimeMS, callbackPendingIntent);  // API level 19 differentiated Set (loose) from SetExact (tight)
                else
#endif
                {
                    alarmManager.Set(AlarmType.RtcWakeup, callbackTimeMS, callbackPendingIntent);  // API 1-18 treats Set as a tight alarm
                }
            }
        }
    }
}
