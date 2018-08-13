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

using Sensus.Context;
using Sensus.Exceptions;
using Sensus.UI;
using Xamarin.Forms;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Threading.Tasks;
using Microsoft.AppCenter.Analytics;
using System.Linq;

namespace Sensus.Notifications
{
    /// <summary>
    /// Exposes the user-facing notification functionality of a platform. Also manages push notifications.
    /// </summary>
    public abstract class Notifier : INotifier
    {
        public const string DISPLAY_PAGE_KEY = "SENSUS-DISPLAY-PAGE";

        private List<PushNotificationRequest> _pushNotificationRequestsToSend;
        private List<PushNotificationRequest> _pushNotificationRequestsToDelete;

        public Notifier()
        {
            _pushNotificationRequestsToSend = new List<PushNotificationRequest>();
            _pushNotificationRequestsToDelete = new List<PushNotificationRequest>();
        }

        public abstract void IssueNotificationAsync(string title, string message, string id, Protocol protocol, bool alertUser, DisplayPage displayPage);

        public abstract void CancelNotification(string id);

        public void OpenDisplayPage(DisplayPage displayPage)
        {
            if (displayPage == DisplayPage.None)
            {
                return;
            }

            SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
            {
                Page desiredTopPage = null;

                if (displayPage == DisplayPage.PendingSurveys)
                {
                    desiredTopPage = new PendingScriptsPage();
                }
                else
                {
                    SensusException.Report("Unrecognized display page:  " + displayPage);
                    return;
                }

                (Application.Current as App).DetailPage = new NavigationPage(desiredTopPage);
            });
        }

        public Task SendPushNotificationRequestAsync(PushNotificationRequest request, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                if (request == null)
                {
                    return;
                }

                try
                {
                    await request.Protocol.RemoteDataStore.SendPushNotificationRequestAsync(request, cancellationToken);

                    lock (_pushNotificationRequestsToSend)
                    {
                        _pushNotificationRequestsToSend.Remove(request);
                    }
                }
                catch (Exception sendException)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while sending push notification request:  " + sendException.Message, LoggingLevel.Normal, GetType());

                    lock (_pushNotificationRequestsToSend)
                    {
                        int currIndex = _pushNotificationRequestsToSend.IndexOf(request);
                        if (currIndex < 0)
                        {
                            _pushNotificationRequestsToSend.Add(request);
                        }
                        else
                        {
                            _pushNotificationRequestsToSend[currIndex] = request;
                        }
                    }
                }
                finally
                {
                    lock (_pushNotificationRequestsToDelete)
                    {
                        _pushNotificationRequestsToDelete.Remove(request);
                    }
                }
            });
        }

        public Task DeletePushNotificationRequestAsync(PushNotificationRequest request, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                try
                {
                    await request.Protocol.RemoteDataStore.DeletePushNotificationRequestAsync(request, cancellationToken);

                    lock (_pushNotificationRequestsToDelete)
                    {
                        _pushNotificationRequestsToDelete.Remove(request);
                    }
                }
                catch (Exception deleteException)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while deleting push notification request:  " + deleteException.Message, LoggingLevel.Normal, GetType());

                    lock (_pushNotificationRequestsToDelete)
                    {
                        int currIndex = _pushNotificationRequestsToDelete.IndexOf(request);
                        if (currIndex < 0)
                        {
                            _pushNotificationRequestsToDelete.Add(request);
                        }
                        else
                        {
                            _pushNotificationRequestsToDelete[currIndex] = request;
                        }
                    }
                }
                finally
                {
                    lock (_pushNotificationRequestsToSend)
                    {
                        _pushNotificationRequestsToSend.Remove(request);
                    }
                }
            });
        }

        public Task TestHealthAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                #region send all outstanding push notification requests
                List<PushNotificationRequest> pushNotifications;
                lock (_pushNotificationRequestsToSend)
                {
                    pushNotifications = _pushNotificationRequestsToSend.ToList();
                }

                foreach (PushNotificationRequest pushNotificationRequestToSend in pushNotifications)
                {
                    try
                    {
                        await SendPushNotificationRequestAsync(pushNotificationRequestToSend, cancellationToken);
                    }
                    catch (Exception sendException)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while sending push notification request:  " + sendException.Message, LoggingLevel.Normal, GetType());
                    }
                }

                // report remaining PNRs to send
                lock (_pushNotificationRequestsToSend)
                {
                    string eventName = TrackedEvent.Health + ":" + GetType().Name;
                    Dictionary<string, string> properties = new Dictionary<string, string>
                    {
                        { "PNRs to Send", _pushNotificationRequestsToSend.Count.ToString() }
                    };

                    Analytics.TrackEvent(eventName, properties);
                }
                #endregion

                #region delete all outstanding push notification requests
                lock (_pushNotificationRequestsToDelete)
                {
                    pushNotifications = _pushNotificationRequestsToDelete.ToList();
                }

                foreach (PushNotificationRequest pushNotificationRequestToDelete in pushNotifications)
                {
                    try
                    {
                        await DeletePushNotificationRequestAsync(pushNotificationRequestToDelete, cancellationToken);
                    }
                    catch (Exception deleteException)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while deleting push notification request:  " + deleteException.Message, LoggingLevel.Normal, GetType());
                    }
                }

                // report remaining PNRs to delete
                lock (_pushNotificationRequestsToDelete)
                {
                    string eventName = TrackedEvent.Health + ":" + GetType().Name;
                    Dictionary<string, string> properties = new Dictionary<string, string>
                    {
                        { "PNRs to Delete", _pushNotificationRequestsToDelete.Count.ToString() }
                    };

                    Analytics.TrackEvent(eventName, properties);
                }
                #endregion
            });
        }
    }
}