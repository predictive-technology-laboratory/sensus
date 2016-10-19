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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Android.App;
using Android.Media;
using Android.Content;
using Sensus.Shared.Context;
using Sensus.Shared.Callbacks;
using Sensus.Shared.Notifications;

#if __ANDROID_23__
using Android.OS;
#endif

namespace Sensus.Shared.Android.Notifications
{
    public class AndroidNotify: INotify
    {
        #region Fields
        private readonly ConcurrentDictionary<string, PendingIntent> _pendingsById;
        private readonly ConcurrentDictionary<string, Action<string, CancellationToken, Action>> _callbacksById;
        private readonly Service _androidService;
        private readonly NotificationManager _notificationManager;
        private readonly AlarmManager _alarmManager;
        private readonly int _smallIcon;
        #endregion

        #region Constructors
        public AndroidNotify(Service androidService, int smallIcon)
        {            
            _androidService      = androidService;
            _pendingsById        = new ConcurrentDictionary<string, PendingIntent>();
            _callbacksById       = new ConcurrentDictionary<string, Action<string, CancellationToken, Action>>();

            _notificationManager = (NotificationManager)androidService.GetSystemService(global::Android.Content.Context.NotificationService);
            _alarmManager        = (AlarmManager)_androidService.GetSystemService(global::Android.Content.Context.AlarmService);

            _smallIcon = smallIcon;
        }
        #endregion

        #region Public Methods
        public void ScheduleNotification(string tag, string message, string title, bool autoCancel, bool ongoing, bool playSound, bool vibrate)
        {
            if (_notificationManager != null)
            {
                Task.Run(() =>
                {
                    if (message == null)
                    { 
                        _notificationManager.Cancel(tag, 0);
                        return;
                    }

                    var intent  = new Intent(_androidService, _androidService.GetType());
                    var pending = PendingIntent.GetService(_androidService, 0, intent, PendingIntentFlags.UpdateCurrent);

                    var builder = new Notification.Builder(_androidService).SetContentTitle(title).SetContentText(message).SetSmallIcon(_smallIcon).SetContentIntent(pending).SetAutoCancel(autoCancel).SetOngoing(ongoing);

                    if (playSound)
                    {
                        builder.SetSound(RingtoneManager.GetDefaultUri(RingtoneType.Notification));
                    }

                    if (vibrate)
                    {
                        builder.SetVibrate(new long[] {0, 250, 50, 250});
                    }

                    _notificationManager.Notify(tag, 0, builder.Build());
                });
            }
        }        

        public string ScheduleCallback(TimeSpan interval, bool lagAllowed, bool repeating, Action<string, CancellationToken, Action> callback)
        {
            var callbackId   = Guid.NewGuid().ToString();
            var callbackTime = DateTime.Now.Add(interval);

            SensusServiceHelper.Get().Logger.Log($"Callback {callbackId} scheduled for {callbackTime} (one-time).", LoggingLevel.Normal, GetType());

            var intent  = Intent(callbackId, repeating, interval, lagAllowed);
            var pending = Pending(intent, callbackId);

            ScheduleCallbackAlarm(pending, callbackId, interval);

            return callbackId;
        }        

        public void UnscheduleCallback(string callbackId)
        {
            PendingIntent pendingIntent;

            if (_pendingsById.TryGetValue(callbackId, out pendingIntent))
            {
                _alarmManager.Cancel(pendingIntent);

                _pendingsById.TryRemove(callbackId, out pendingIntent);
            }
        }

        public void ExecuteCallback(INotifyMeta meta, Intent intent)
        {
            // if the user removes the main activity from the switcher, the service's process will be killed and restarted without notice, and 
            // we'll have no opportunity to unschedule repeating callbacks. when the service is restarted we'll reinitialize the service
            // helper, restart the repeating callbacks, and we'll then have duplicate repeating callbacks. handle the invalid callbacks below.
            // if the callback is scheduled, it's fine. if it's not, then unschedule it.
            var wakeLockReleased = false;

            var serviceHelper = SensusServiceHelper.Get();
            var scheduleHelper = (CallbackScheduler)SensusContext.Current.Notifier;

            // raise callback and notify the user if there is a message. we wouldn't have presented the user with the message yet.
            scheduleHelper.RaiseCallbackAsync(meta, true,

                // schedule a new callback at the given time.
                repeatCallbackTime =>
                {
                    ScheduleCallbackAlarm(Pending(intent, meta.CallbackId), meta.CallbackId, meta.RepeatDelay);
                },

                // if the callback indicates that it's okay for the device to sleep, release the wake lock now.
                () =>
                {
                    wakeLockReleased = true;
                    serviceHelper.LetDeviceSleep();
                    serviceHelper.Logger.Log("Wake lock released preemptively for scheduled callback action.", LoggingLevel.Normal, null);
                },

                // release wake lock now if we didn't while the callback action was executing.
                () =>
                {
                    if (!wakeLockReleased)
                    {
                        serviceHelper.LetDeviceSleep();
                        serviceHelper.Logger.Log("Wake lock released after scheduled callback action completed.", LoggingLevel.Normal, null);
                    }
                }
            );
        }
        #endregion

        #region Private Methods
        private Intent Intent(string callbackId, bool repeating, TimeSpan delay, bool lagAllowed)
        {
            var intent = new Intent(_androidService, _androidService.GetType());

            new AndroidNotifyMeta(intent)
            {
                Type        = NotificationType.Callback,
                CallbackId  = callbackId,
                IsRepeating = repeating,
                RepeatDelay = delay,
                LagAllowed  = lagAllowed
            };

            return intent;
        }

        private PendingIntent Pending(Intent callbackIntent, string callbackId)
        {            
            return global::Android.App.PendingIntent.GetService(_androidService, callbackId.GetHashCode(), callbackIntent, PendingIntentFlags.CancelCurrent);
        }

        private void ScheduleCallbackAlarm(PendingIntent pendingIntent, string callbackId, TimeSpan interval)
        {
            _pendingsById.AddOrUpdate(callbackId, pendingIntent, (s,v) => v);
            
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
    }
}
