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

using Android.App;
using Android.Content;
using Android.OS;
using Sensus.Callbacks;
using System.Threading.Tasks;
using Sensus.Extensions;
using Sensus.Notifications;

namespace Sensus.Android.Callbacks
{
    public class AndroidCallbackScheduler : CallbackScheduler
    {
        private AndroidSensusService _service;

        public AndroidCallbackScheduler(AndroidSensusService service)
        {
            _service = service;
        }

        protected override Task RequestLocalInvocationAsync(ScheduledCallback callback)
        {
            Intent callbackIntent = CreateCallbackIntent(callback);
            PendingIntent callbackPendingIntent = CreateCallbackPendingIntent(callbackIntent);
            ScheduleCallbackAlarm(callback, callbackPendingIntent);
            SensusServiceHelper.Get().Logger.Log("Callback " + callback.Id + " scheduled for " + callback.NextExecution + " " + (callback.RepeatDelay.HasValue ? "(repeating)" : "(one-time)") + ".", LoggingLevel.Normal, GetType());
            return Task.CompletedTask;
        }

        private Intent CreateCallbackIntent(ScheduledCallback callback)
        {
            Intent callbackIntent = new Intent(_service, typeof(AndroidSensusService));
            callbackIntent.SetAction(callback.Id);
            callbackIntent.PutExtra(Notifier.NOTIFICATION_USER_RESPONSE_ACTION_KEY, callback.NotificationUserResponseAction.ToString());
            callbackIntent.PutExtra(SENSUS_CALLBACK_KEY, true);
            callbackIntent.PutExtra(SENSUS_CALLBACK_INVOCATION_ID_KEY, callback.InvocationId);
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

        private void ScheduleCallbackAlarm(ScheduledCallback callback, PendingIntent callbackPendingIntent)
        {
            AlarmManager alarmManager = _service.GetSystemService(global::Android.Content.Context.AlarmService) as AlarmManager;

            // cancel the current alarm for this callback id. this deals with the situations where (1) sensus schedules callbacks
            // and is subsequently restarted, or (2) a probe is restarted after scheduling callbacks (e.g., script runner). in 
            // these situations, the originally scheduled callbacks will remain in the alarm manager, and we'll begin piling up
            // additional, duplicate alarms. we need to be careful to avoid duplicate alarms, and this is how we manage it.
            alarmManager.Cancel(callbackPendingIntent);

            long callbackTimeMS = callback.NextExecution.Value.ToJavaCurrentTimeMillis();

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

            SensusServiceHelper.Get().Logger.Log("Alarm scheduled for callback " + callback.Id + " at " + callback.NextExecution.Value + ".", LoggingLevel.Normal, GetType());
        }

        public ScheduledCallback TryGetCallback(Intent intent)
        {
            if (IsCallback(intent))
            {
                return TryGetCallback(intent.Action);
            }
            else
            {
                return null;
            }
        }

        public bool IsCallback(Intent intent)
        {
            return intent.GetBooleanExtra(SENSUS_CALLBACK_KEY, false);
        }

        public async Task ServiceCallbackAsync(Intent intent)
        {
            ScheduledCallback callback = TryGetCallback(intent.Action);

            if (callback == null)
            {
                return;
            }

            SensusServiceHelper serviceHelper = SensusServiceHelper.Get();

            serviceHelper.Logger.Log("Servicing callback " + callback.Id + ".", LoggingLevel.Normal, GetType());

            // if the user removes the main activity from the switcher, the service's process will be killed and restarted without notice, and 
            // we'll have no opportunity to unschedule repeating callbacks. when the service is restarted we'll reinitialize the service
            // helper, restart the repeating callbacks, and we'll then have duplicate repeating callbacks. handle the invalid callbacks below.
            // if the callback is present, it's fine. if it's not, then unschedule it.
            if (ContainsCallback(callback))
            {
                string invocationId = intent.GetStringExtra(SENSUS_CALLBACK_INVOCATION_ID_KEY);

                // raise callback and notify the user if there is a message. we wouldn't have presented the user with the message yet.
                await RaiseCallbackAsync(callback, invocationId, true);
            }
            else
            {
                await UnscheduleCallbackAsync(callback);
            }
        }

        public override async Task ServiceCallbackAsync(ScheduledCallback callback, string invocationId)
        {
            // service an intent that targets the given callback and invocation. 
            // 
            // 1) if this intent arrives with a valid invocation ID before the alarm-triggered 
            //    intent arrives, this intent will be serviced and a new pending intent will 
            //    be issued with an updated invocation id. in this case, the alarm-triggered 
            //    pending intent will be ignored (if it fires) or canceled (when the updated 
            //    pending intent is issued).
            //
            // 2) if this intent arrives after the alarm-triggered intent, or if the invocation
            //    id is not valid, then this intent will not be serviced. the alarm-triggered
            //    intent will be serviced instead, and the next pending intent will be scheduled
            //    thereafter along with a correspondingly new push notification request.
            Intent intent = CreateCallbackIntent(callback);
            intent.PutExtra(SENSUS_CALLBACK_INVOCATION_ID_KEY, invocationId);
            await ServiceCallbackAsync(intent);
        }

        protected override void CancelLocalInvocation(ScheduledCallback callback)
        {
            // we don't need a reference to the original pending intent in order to cancel the alarm. we just need a pending intent
            // with the same request code and underlying intent with the same action. extras are not considered, and we don't use
            // the other intent fields (data, categories, etc.). see the following for more information:
            //
            // https://developer.android.com/reference/android/app/PendingIntent.html 
            //
            Intent callbackIntent = CreateCallbackIntent(callback);
            PendingIntent callbackPendingIntent = CreateCallbackPendingIntent(callbackIntent);
            AlarmManager alarmManager = _service.GetSystemService(global::Android.Content.Context.AlarmService) as AlarmManager;
            alarmManager.Cancel(callbackPendingIntent);
            SensusServiceHelper.Get().Logger.Log("Unscheduled alarm for callback " + callback.Id + ".", LoggingLevel.Normal, GetType());
        }
    }
}