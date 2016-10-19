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
using System.Linq;

using Sensus.Shared.UI;
using Sensus.Shared.Context;
using Sensus.Shared.Callbacks;
using Sensus.Shared.Notifications;
using Sensus.Shared.iOS.Notifications;

using UIKit;
using Xamarin.Forms;

namespace Sensus.Shared.iOS.Callbacks
{
    public abstract class iOSCallbackScheduler : CallbackScheduler, IiOSCallbackScheduler
    {
        public const string SENSUS_CALLBACK_ACTIVATION_ID_KEY = "SENSUS-CALLBACK-ACTIVATION-ID";

        protected override void ScheduleRepeatingCallback(string callbackId, int initialDelayMS, int repeatDelayMS, bool repeatLag)
        {
            ScheduleCallbackAsync(new iOSNotifyMeta { CallbackId = callbackId, RepeatDelay = TimeSpan.FromMilliseconds(initialDelayMS), IsRepeating = true, LagAllowed = repeatLag });
        }

        protected override void ScheduleOneTimeCallback(string callbackId, int delayMS)
        {
            ScheduleCallbackAsync(new iOSNotifyMeta { CallbackId = callbackId, RepeatDelay = TimeSpan.FromMilliseconds(delayMS), IsRepeating = false, LagAllowed = false });
        }

        protected abstract void ScheduleCallbackAsync(INotifyMeta meta);

        public abstract void UpdateCallbackActivationIdsAsync(string newActivationId);

        public void ServiceCallbackAsync(iOSNotifyMeta meta)
        {            
            // only raise callback if it's from the current activation and if it is still scheduled
            if (meta.ActivationId != SensusContext.Current.ActivationId || !CallbackIsScheduled(meta.CallbackId)) return;

            if (meta.Type == NotificationType.Callback)
            {
                nint callbackTaskId = UIApplication.SharedApplication.BeginBackgroundTask(() => { CancelRaisedCallback(meta.CallbackId); /* if we're out of time running in the background, cancel the callback */ });

                // raise callback but don't notify user since we would have already done so when the UILocalNotification was delivered to the notification tray.
                RaiseCallbackAsync(meta, false,

                    // don't need to specify how repeats will be scheduled. the class that extends this one will take care of it.
                    null,

                    // nothing to do if the callback thinks we can sleep. ios does not provide 
                    null,

                    // we've completed the raising process, so end background task
                    () =>
                    {
                        SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                        {
                            UIApplication.SharedApplication.EndBackgroundTask(callbackTaskId);
                        });
                    }
                );
            }

            // check whether the user opened a pending-survey notification (indicated by an application state that is not active). we'll
            // also get notifications when the app is active, due to how we manage pending-survey notifications.
            if (meta.Type == NotificationType.Script && UIApplication.SharedApplication.ApplicationState != UIApplicationState.Active)
            {                
                // display the pending scripts page if it is not already on the top of the navigation stack
                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                {
                    var topPage = Application.Current.MainPage.Navigation.NavigationStack.LastOrDefault();

                    if (!(topPage is PendingScriptsPage))
                    {
                        await Application.Current.MainPage.Navigation.PushAsync(new PendingScriptsPage());
                    }
                });
            }
        }
    }
}
