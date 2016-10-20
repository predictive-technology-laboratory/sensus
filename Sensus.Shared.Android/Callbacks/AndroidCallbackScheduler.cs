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
using System.Collections.Concurrent;

using Android.App;
using Android.Content;

using Sensus.Shared.Callbacks;

#if __ANDROID_23__
using Android.OS;
#endif

namespace Sensus.Shared.Android.Callbacks
{
    public class AndroidCallbackScheduler: CallbackScheduler
    {
        #region Fields
        private readonly Service _androidService;
        private readonly ConcurrentDictionary<string, PendingIntent> _callbackIdPendingIntent;        
        private readonly AlarmManager _alarmManager;
        #endregion

        #region Constructors
        public AndroidCallbackScheduler(Service androidService)
        {
            _androidService          = androidService;
            _callbackIdPendingIntent = new ConcurrentDictionary<string, PendingIntent>();
            _alarmManager            = (AlarmManager)_androidService.GetSystemService(global::Android.Content.Context.AlarmService);
        }
        #endregion

        #region Public Methods
        private void ScheduleCallbackAlarm(PendingIntent pendingIntent, string callbackId, TimeSpan interval)
        {
            _callbackIdPendingIntent.AddOrUpdate(callbackId, pendingIntent, (s, v) => v);

            var callbackTimeMS = Java.Lang.JavaSystem.CurrentTimeMillis() + (long)interval.TotalDays;

#if __ANDROID_23__
            // https://github.com/predictive-technology-laboratory/sensus/wiki/Backwards-Compatibility
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                _alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, callbackTimeMS, pendingIntent); // API level 23 added "while idle" option, making things even tighter.
            }
            else if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
            {
                _alarmManager.SetExact(AlarmType.RtcWakeup, callbackTimeMS, pendingIntent); // API level 19 differentiated Set (loose) from SetExact (tight)
            }
            else
            {
                _alarmManager.Set(AlarmType.RtcWakeup, callbackTimeMS, pendingIntent); // API 1-18 treats Set as a tight alarm
            }
#else
            _alarmManager.Set(AlarmType.RtcWakeup, callbackTimeMS, pendingIntent); // API 1-18 treats Set as a tight alarm
#endif
        }
        #endregion

        #region Protected Methods
        protected override void ScheduleRepeatingCallback(string callbackId, int initialDelayMS, int repeatDelayMS, bool repeatLag)
        {
            var callbackTime = DateTime.Now.AddMilliseconds(initialDelayMS);

            SensusServiceHelper.Get().Logger.Log("Callback " + callbackId + " scheduled for " + callbackTime + " (repeating).", LoggingLevel.Normal, GetType());

            var intent  = NewIntent(callbackId, true, TimeSpan.FromMilliseconds(initialDelayMS), repeatLag);
            var pending = NewPending(intent, callbackId);

            ScheduleCallbackAlarm(pending, callbackId, TimeSpan.FromMilliseconds(repeatDelayMS));
        }

        protected override void ScheduleOneTimeCallback(string callbackId, int delayMS)
        {
            var callbackTime = DateTime.Now.AddMilliseconds(delayMS);

            SensusServiceHelper.Get().Logger.Log("Callback " + callbackId + " scheduled for " + callbackTime + " (one-time).", LoggingLevel.Normal, GetType());

            var intent  = NewIntent(callbackId, false, TimeSpan.Zero, false);
            var pending = NewPending(intent, callbackId);

            ScheduleCallbackAlarm(pending, callbackId, TimeSpan.FromMilliseconds(delayMS));
        }

        protected override void UnscheduleCallbackPlatformSpecific(string callbackId)
        {
            PendingIntent callbackPendingIntent;
            if (_callbackIdPendingIntent.TryRemove(callbackId, out callbackPendingIntent))
            {                    
                _alarmManager.Cancel(callbackPendingIntent);                    
            }
        }
        #endregion

        #region Private Methods
        private Intent NewIntent(string callbackId, bool repeating, TimeSpan delay, bool lagAllowed)
        {
            var intent = new Intent(_androidService, _androidService.GetType());

            new AndroidCallbackData(intent)
            {
                Type        = NotificationType.Callback,
                CallbackId  = callbackId,
                IsRepeating = repeating,
                RepeatDelay = delay,
                LagAllowed  = lagAllowed
            };

            return intent;
        }

        private PendingIntent NewPending(Intent callbackIntent, string callbackId)
        {
            return PendingIntent.GetService(_androidService, callbackId.GetHashCode(), callbackIntent, PendingIntentFlags.CancelCurrent);
        }
        #endregion
    }
}
