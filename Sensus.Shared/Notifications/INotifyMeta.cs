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
using Sensus.Shared.Callbacks;
using Sensus.Shared.Context;
using Sensus.Shared.UI;

namespace Sensus.Shared.Notifications
{
    public interface INotifyMeta
    {        
        NotificationType Type { get; set; } //public const string SENSUS_CALLBACK_KEY = "SENSUS-CALLBACK" || "NOTIFICATION_ID_KEY";
        string CallbackId { get; set;  } //public const string SENSUS_CALLBACK_ID_KEY = "SENSUS-CALLBACK-ID";
        bool IsRepeating { get; set; } //public const string SENSUS_CALLBACK_REPEATING_KEY = "SENSUS-CALLBACK-REPEATING";                
        TimeSpan RepeatDelay { get; set; } //public const string SENSUS_CALLBACK_REPEAT_DELAY_KEY = "SENSUS-CALLBACK-REPEAT-DELAY";

        /// <summary>
        /// if lag is allowed call backs are scheduled to run until their original finish time.
        /// otherwise, schedule the repeat from the time at which the current callback was raised.
        /// </summary>
        bool LagAllowed { get; set; } //public const string SENSUS_CALLBACK_REPEAT_LAG_KEY = "SENSUS-CALLBACK-REPEAT-LAG";
    }

    public static class INotifyMetaExtensions
    {
        public static void Execute(this INotifyMeta meta)
        {
            var serviceHelper  = SensusServiceHelper.Get();
            var scheduleHelper = (CallbackScheduler)SensusContext.Current.CallbackScheduler;

            if (meta == null)
            {
                serviceHelper.LetDeviceSleep();
                return;
            }

            if (meta.Type == NotificationType.Undefined)
            {
                serviceHelper.LetDeviceSleep();
                return;
            }

            if (meta.Type == NotificationType.Callback && !scheduleHelper.CallbackIsScheduled(meta.CallbackId))
            {
                scheduleHelper.UnscheduleCallback(meta.CallbackId);
                serviceHelper.LetDeviceSleep();
            }

            if(meta.Type == NotificationType.Callback && scheduleHelper.CallbackIsScheduled(meta.CallbackId))
            { 
            }

            if (meta.Type == NotificationType.Script)
            {
                serviceHelper.BringToForeground();

                // display the pending scripts page if it is not already on the top of the navigation stack
                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
                {
                    var topPage = Xamarin.Forms.Application.Current.MainPage.Navigation.NavigationStack.LastOrDefault();

                    if (!(topPage is PendingScriptsPage))
                    {
                        await Xamarin.Forms.Application.Current.MainPage.Navigation.PushAsync(new PendingScriptsPage());
                    }

                    serviceHelper.LetDeviceSleep();
                });
            }
        }
    }
}
