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
using Android.App;
using Android.Content;
using Android.OS;
using Sensus.Callbacks;
using System.Threading.Tasks;

namespace Sensus.Android.Callbacks
{
    public class AndroidCallbackScheduler : CallbackScheduler
    {
        private AndroidSensusService _service;

        public AndroidCallbackScheduler(AndroidSensusService service)
        {
            _service = service;
        }

        protected override void ScheduleOneTimeCallbackPlatformSpecific(string callbackId, TimeSpan delay)
        {
            Intent callbackIntent = CreateCallbackIntent(callbackId);
            PendingIntent callbackPendingIntent = CreateCallbackPendingIntent(callbackIntent);
            ScheduleCallbackAlarm(callbackPendingIntent, delay);
            SensusServiceHelper.Get().Logger.Log("Callback " + callbackId + " scheduled for " + (DateTime.Now + delay) + " (one-time).", LoggingLevel.Normal, GetType());
        }

        protected override void ScheduleRepeatingCallbackPlatformSpecific(string callbackId, TimeSpan initialDelay, TimeSpan repeatDelay, bool repeatLag)
        {
            Intent callbackIntent = CreateCallbackIntent(callbackId);
            callbackIntent.PutExtra(SENSUS_CALLBACK_REPEATING_KEY, true);
            callbackIntent.PutExtra(SENSUS_CALLBACK_REPEAT_DELAY_KEY, repeatDelay.Ticks.ToString());
            callbackIntent.PutExtra(SENSUS_CALLBACK_REPEAT_LAG_KEY, repeatLag);
            PendingIntent callbackPendingIntent = CreateCallbackPendingIntent(callbackIntent);
            ScheduleCallbackAlarm(callbackPendingIntent, initialDelay);
            SensusServiceHelper.Get().Logger.Log("Callback " + callbackId + " scheduled for " + (DateTime.Now + initialDelay) + " (repeating).", LoggingLevel.Normal, GetType());
        }

        private Intent CreateCallbackIntent(string callbackId)
        {
            Intent callbackIntent = new Intent(_service, typeof(AndroidSensusService));
            callbackIntent.SetAction(callbackId);
            callbackIntent.PutExtra(Notifier.DISPLAY_PAGE_KEY, GetCallbackDisplayPage(callbackId).ToString());
            callbackIntent.PutExtra(SENSUS_CALLBACK_KEY, true);
            return callbackIntent;
        }

        private PendingIntent CreateCallbackPendingIntent(Intent callbackIntent)
        {
            // intent extras are not considered when checking equality. thus, the only way we'll get equal pending intents is
            // if the intent action (i.e., callback id) is the same. this should not happen, but if it does we should probably
            // cancel the previously issued (current) intent with the one we are here constructing and are about to pass to the
            // alarm manager. see the following for more information:
            //
            // https://developer.android.com/reference/android/app/PendingIntent.html 
            //
            return PendingIntent.GetService(_service, 0, callbackIntent, PendingIntentFlags.CancelCurrent);
        }

        private void ScheduleCallbackAlarm(PendingIntent callbackPendingIntent, TimeSpan delay)
        {
            AlarmManager alarmManager = _service.GetSystemService(global::Android.Content.Context.AlarmService) as AlarmManager;

            // cancel the current alarm for this callback id. this deals with the situations where (1) sensus schedules callbacks
            // and is subsequently restarted, or (2) a probe is restarted after scheduling callbacks (e.g., script runner). in 
            // these situations, the originally scheduled callbacks will remain in the alarm manager, and we'll begin piling up
            // additional, duplicate alarms. we need to be careful to avoid duplicate alarms, and this is how we manage it.
            alarmManager.Cancel(callbackPendingIntent);

            long callbackTimeMS = Java.Lang.JavaSystem.CurrentTimeMillis() + (long)delay.TotalMilliseconds;

            // see the Backwards Compatibility article for more information
#if __ANDROID_23__
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, callbackTimeMS, callbackPendingIntent);  // API level 23 added "while idle" option, making things even tighter.
            }
            else if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                alarmManager.SetExact(AlarmType.RtcWakeup, callbackTimeMS, callbackPendingIntent);  // API level 19 differentiated Set (loose) from SetExact (tight)
            }
            else
#endif
            {
                alarmManager.Set(AlarmType.RtcWakeup, callbackTimeMS, callbackPendingIntent);  // API 1-18 treats Set as a tight alarm
            }
        }

        public Task ServiceCallbackAsync(Intent intent)
        {
            return Task.Run(async () =>
            {
                SensusServiceHelper serviceHelper = SensusServiceHelper.Get();

                string callbackId = intent.Action;

                // if the user removes the main activity from the switcher, the service's process will be killed and restarted without notice, and 
                // we'll have no opportunity to unschedule repeating callbacks. when the service is restarted we'll reinitialize the service
                // helper, restart the repeating callbacks, and we'll then have duplicate repeating callbacks. handle the invalid callbacks below.
                // if the callback is scheduled, it's fine. if it's not, then unschedule it.
                if (CallbackIsScheduled(callbackId))
                {
                    bool repeating = intent.GetBooleanExtra(SENSUS_CALLBACK_REPEATING_KEY, false);

                    TimeSpan repeatDelay = TimeSpan.Zero;
                    if (repeating)
                    {
                        repeatDelay = TimeSpan.FromTicks(long.Parse(intent.GetStringExtra(SENSUS_CALLBACK_REPEAT_DELAY_KEY)));
                    }

                    bool repeatLag = intent.GetBooleanExtra(SENSUS_CALLBACK_REPEAT_LAG_KEY, false);
                    bool wakeLockReleased = false;

                    // raise callback and notify the user if there is a message. we wouldn't have presented the user with the message yet.
                    await RaiseCallbackAsync(callbackId, repeating, repeatDelay, repeatLag, true,

                        // schedule a new alarm for the same callback at the desired time.
                        repeatCallbackTime =>
                        {
                            ScheduleCallbackAlarm(CreateCallbackPendingIntent(intent), repeatCallbackTime - DateTime.Now);
                        },

                        // if the callback indicates that it's okay for the device to sleep, release the wake lock now.
                        () =>
                        {
                            wakeLockReleased = true;
                            serviceHelper.LetDeviceSleep();
                            serviceHelper.Logger.Log("Wake lock released preemptively for scheduled callback action.", LoggingLevel.Normal, GetType());
                        }
                    );

                    // release wake lock now if we didn't while the callback action was executing.
                    if (!wakeLockReleased)
                    {
                        serviceHelper.LetDeviceSleep();
                        serviceHelper.Logger.Log("Wake lock released after scheduled callback action completed.", LoggingLevel.Normal, GetType());
                    }
                }
                else
                {
                    UnscheduleCallback(callbackId);
                    serviceHelper.LetDeviceSleep();
                }
            });
        }

        protected override void UnscheduleCallbackPlatformSpecific(string callbackId)
        {
            // we don't need a reference to the original pending intent in order to cancel the alarm. we just need a pending intent
            // with the same request code and underlying intent with the same action. extras are not considered, and we don't use
            // the other intent fields (data, categories, etc.). see the following for more information:
            //
            // https://developer.android.com/reference/android/app/PendingIntent.html 
            //
            Intent callbackIntent = CreateCallbackIntent(callbackId);
            PendingIntent callbackPendingIntent = CreateCallbackPendingIntent(callbackIntent);
            AlarmManager alarmManager = _service.GetSystemService(global::Android.Content.Context.AlarmService) as AlarmManager;
            alarmManager.Cancel(callbackPendingIntent);
            SensusServiceHelper.Get().Logger.Log("Unscheduled alarm for callback \"" + callbackId + "\".", LoggingLevel.Normal, GetType());
        }
    }
}