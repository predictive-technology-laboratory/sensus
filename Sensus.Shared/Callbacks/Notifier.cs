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
using Sensus.Context;
using Sensus.Exceptions;
using Sensus.UI;
using Xamarin.Forms;

namespace Sensus.Callbacks
{
    public abstract class Notifier : INotifier
    {
        public const string DISPLAY_PAGE_KEY = "SENSUS-DISPLAY-PAGE";

        public abstract void IssueNotificationAsync(string title, string message, string id, string protocolId, bool vibrateAndPlaySound, DisplayPage displayPage);

        public abstract void CancelNotification(string id);

        public void OpenDisplayPage(DisplayPage displayPage)
        {
            if (displayPage == DisplayPage.None)
                return;

            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(async () =>
            {
                Page desiredTopPage = null;

                if (displayPage == DisplayPage.PendingSurveys)
                    desiredTopPage = new PendingScriptsPage();
                else
                {
                    SensusException.Report("Unrecognized display page:  " + displayPage);
                    return;
                }

                Page currentTopPage = Application.Current.MainPage.Navigation.NavigationStack.LastOrDefault();

                if (currentTopPage == null || desiredTopPage.GetType() != currentTopPage.GetType())
                    await Application.Current.MainPage.Navigation.PushAsync(desiredTopPage);
            });
        }

        public static bool NotificationTimeIsWithinAlertExclusionWindow(string protocolId, DateTime time)
        {
            var runningProtocol = SensusServiceHelper.Get().GetRunningProtocolById(protocolId);

            if (runningProtocol != null)
                foreach (Window window in runningProtocol.NotificationAlertExclusionWindowsList)
                    if (window.EncompassesTime(time))
                        return true;

            return false;
        }
    }
}