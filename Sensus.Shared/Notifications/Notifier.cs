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
using Sensus.Callbacks;

namespace Sensus.Notifications
{
    /// <summary>
    /// Exposes the user-facing notification functionality of a platform. Also manages push notifications.
    /// </summary>
    public abstract class Notifier
    {
        public const string DISPLAY_PAGE_KEY = "SENSUS-DISPLAY-PAGE";

        private List<PushNotificationRequest> _pushNotificationRequestsToSend;
        private List<PushNotificationRequest> _pushNotificationRequestsToDelete;

        public Notifier()
        {
            _pushNotificationRequestsToSend = new List<PushNotificationRequest>();
            _pushNotificationRequestsToDelete = new List<PushNotificationRequest>();
        }

        public abstract Task IssueNotificationAsync(string title, string message, string id, Protocol protocol, bool alertUser, DisplayPage displayPage);

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
                if (request == null)
                {
                    return;
                }

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

        public Task ProcessReceivedPushNotificationAsync(string protocolId, string id, string title, string body, string sound, string command, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                // every push notification should have an ID
                try
                {
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        throw new Exception("Push notification ID is missing or blank.");
                    }
                }
                catch (Exception ex)
                {
                    SensusException.Report("Exception while getting push notification id:  " + ex.Message, ex);
                    return;
                }

                SensusServiceHelper.Get().Logger.Log("Processing push notification " + id, LoggingLevel.Normal, GetType());

                // every push notification should target a protocol
                Protocol protocol = null;
                try
                {
                    protocol = SensusServiceHelper.Get().RegisteredProtocols.Single(p => p.Id == protocolId);
                }
                catch (Exception ex)
                {
                    SensusException.Report("Failed to get protocol for push notification:  " + ex.Message, ex);
                    return;
                }

                // ignore the push notification if it targets a protocol that is not running and is not 
                // scheduled to run. we explicitly attempt to prevent such notifications from coming through 
                // by unregistering from hubs that lack running/scheduled protocols and clearing the token 
                // from the backend; however, there may be race conditions that allow a push notification 
                // to be delivered to us nonetheless.
                if (!protocol.Running && !protocol.StartIsScheduled)
                {
                    SensusServiceHelper.Get().Logger.Log("Protocol targeted by push notification is not running and is not scheduled to run.", LoggingLevel.Normal, GetType());
                    return;
                }

#if __ANDROID__
                // if there is user-targeted information, display the notification. this only applies to android because 
                // push notifications are automatically displayed on iOS when the app is in the background. when the
                // app is in the foreground it doesn't make sense to display the notification.
                try
                {
                    if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(body))
                    {
                        await IssueNotificationAsync(title, body, id, protocol, !string.IsNullOrWhiteSpace(sound), DisplayPage.None);
                    }
                }
                catch (Exception ex)
                {
                    SensusException.Report("Exception while notifying from push notification:  " + ex.Message, ex);
                }
#endif

                // process push notification command if there is one
                try
                {
                    string[] commandParts = command.Split(new char[] { '|' });

                    if (commandParts.Length > 0)
                    {
                        if (commandParts.First() == CallbackScheduler.SENSUS_CALLBACK_KEY)
                        {
                            if (commandParts.Length != 4)
                            {
                                throw new Exception("Invalid push notification callback command format:  " + command);
                            }

                            string callbackId = commandParts[2];
                            string invocationId = commandParts[3];

                            await SensusContext.Current.CallbackScheduler.ServiceCallbackFromPushNotificationAsync(callbackId, invocationId, cancellationToken);

                            // cancel any local notification associated with the callback (e.g., the notification 
                            // that prompts for polling readings). this only applies to ios, as there are no such
                            // notifications on android.
#if __IOS__
                            SensusContext.Current.Notifier.CancelNotification(callbackId);
#endif

                        }
                        else
                        {
                            throw new Exception("Unrecognized push notification command prefix:  " + commandParts.First());
                        }
                    }
                }
                catch (Exception pushNotificationCommandException)
                {
                    SensusException.Report("Exception while running push notification command:  " + pushNotificationCommandException.Message, pushNotificationCommandException);
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

                SensusServiceHelper.Get().Logger.Log("Sending " + pushNotifications.Count + " outstanding push notification request(s).", LoggingLevel.Normal, GetType());

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

                SensusServiceHelper.Get().Logger.Log("Deleting " + pushNotifications.Count + " outstanding push notification request(s).", LoggingLevel.Normal, GetType());

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