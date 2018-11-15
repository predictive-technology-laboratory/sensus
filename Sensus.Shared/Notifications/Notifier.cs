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
using Sensus.Probes.User.Scripts;
using Sensus.Probes;

namespace Sensus.Notifications
{
    /// <summary>
    /// Exposes the user-facing notification functionality of a platform. Also manages push notifications.
    /// </summary>
    public abstract class Notifier
    {
        public const string DISPLAY_PAGE_KEY = "SENSUS-DISPLAY-PAGE";
        private const string UPDATE_SCRIPT_AGENT_POLICY_COMMAND = "UPDATE-EMA-POLICY";

        private List<PushNotificationRequest> _pushNotificationRequestsToSend;

        /// <summary>
        /// Whether trying to delete a push notification request, we don't always have the
        /// original <see cref="PushNotificationRequest"/> object. This happens, for example,
        /// when we receive the push notification. All we have in that case is the ID and 
        /// protocol. So just track this information.
        /// </summary>
        private List<Tuple<string, Protocol>> _pushNotificationRequestsToDelete;

        public Notifier()
        {
            _pushNotificationRequestsToSend = new List<PushNotificationRequest>();
            _pushNotificationRequestsToDelete = new List<Tuple<string, Protocol>>();
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

        public async Task SendPushNotificationRequestAsync(PushNotificationRequest request, CancellationToken cancellationToken)
        {
            // request can be null (e.g., if being called when working with the app-level health test callback, which has no associated protocol)
            if (request == null)
            {
                return;
            }

            // if the PNR targets the current device and the protocol isn't listening, the don't send the request. this 
            // will eliminate unnecessary network traffic and prevent invalid PNRs from accumulating in the backend.
            if (request.DeviceId == SensusServiceHelper.Get().DeviceId)
            {
                if (string.IsNullOrWhiteSpace(request.Protocol.PushNotificationsHub) || string.IsNullOrWhiteSpace(request.Protocol.PushNotificationsSharedAccessSignature))
                {
                    SensusServiceHelper.Get().Logger.Log("PNR targets current device, which is not listening for PNs. Not sending PNR.", LoggingLevel.Normal, GetType());
                    return;
                }
            }

            // send the push notification request to the remote data store
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

                // hang on to the push notification for sending in the future, e.g., when internet is restored.
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
                // we just sent the push notification request, so it doesn't make sense for there to be a pending push notification
                // request to delete. remove any pending push notifications to delete that match the passed id.
                lock (_pushNotificationRequestsToDelete)
                {
                    _pushNotificationRequestsToDelete.RemoveAll(idProtocol => idProtocol.Item1 == request.Id);
                }
            }
        }

        public async Task ProcessReceivedPushNotificationAsync(string protocolId, string id, string title, string body, string sound, string command, CancellationToken cancellationToken)
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
                SensusException.Report("Failed to get protocol with id " + protocolId + ":  " + ex.Message, ex);
                return;
            }

            // delete the push notification request from the backend. it used to be that the backend
            // deleted push notification requests after they were delivered; however, this isn't a 
            // good idea because deliveries sometimes fail...maybe the device has no internet
            // connection, maybe the push service is down or fails, etc. in such cases, it used
            // to be that the push notification would never be retried. by waiting for the push
            // notification to arrive and having the app delete the request, we ensure that any
            // such failures will cause the push notification to be retried.
            try
            {
                await DeletePushNotificationRequestAsync(id, protocol, cancellationToken);
            }
            catch (Exception ex)
            {
                // report the failure but do not return from the method. we can still hopefully execute the
                // push notification request. the backend will eventually retry the push notification, which
                // should be ignored by the app or filtered out by the backend prior to delivery.
                SensusException.Report("Failed to delete push notification from backend:  " + ex.Message, ex);
            }

            // if the targeted protocol is not running, do some digging.
            if (!protocol.Running)
            {
                // the protocol is scheduled to start in the future. as the push notification should be the start command itself, 
                // we should allow the push notification processing to continue.
                if (protocol.StartIsScheduled)
                {
                    SensusServiceHelper.Get().Logger.Log("Push notification targets protocol that has not started but is scheduled to do so. Allowing.", LoggingLevel.Normal, GetType());
                }
                // the protocol should be running but is not. this can happen if the app dies while 
                // the protocol is running (e.g., due to system killing it or an exception) and 
                // is subsequently restarted (e.g., due to resource pressure alleviation or the 
                // arrival of a push notification). attempt to start the protocol.
                else if (SensusServiceHelper.Get().RunningProtocolIds.Contains(protocol.Id))
                {
                    SensusServiceHelper.Get().Logger.Log("Push notification targets a protocol that is not running but should be. Starting protocol.", LoggingLevel.Normal, GetType());
                    await protocol.StartAsync();
                }
                else
                {
                    // the protocol is not running, should not be, and never will be. we explicitly attempt to 
                    // prevent such notifications from coming through by unregistering from hubs that lack 
                    // running/scheduled protocols and clearing the token from the backend; however, there may 
                    // be race conditions (e.g., stopping the protocol just before the arrival of a push 
                    // notification) that allow a push notification to be delivered to us nonetheless.
                    SensusServiceHelper.Get().Logger.Log("Protocol targeted by push notification is not running and is not scheduled to run.", LoggingLevel.Normal, GetType());
                    return;
                }
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

                        // cancel any local notification associated with the callback (e.g., the notification 
                        // that prompts for polling readings). this only applies to ios, as there are no such
                        // notifications on android. furthermore, we need to do this before servicing the 
                        // callback below, as the servicing routines will typically schedule a new poll with
                        // new local/remote notifications. if we cancel the notification after servicing, 
                        // we will end up cancelling the new notification rather than the current one (found
                        // this out the hard way!).
#if __IOS__
                        SensusContext.Current.Notifier.CancelNotification(callbackId);
#endif

                        await SensusContext.Current.CallbackScheduler.ServiceCallbackFromPushNotificationAsync(callbackId, invocationId, cancellationToken);
                    }
                    else if (commandParts.First() == UPDATE_SCRIPT_AGENT_POLICY_COMMAND)
                    {
                        if (protocol.TryGetProbe(typeof(ScriptProbe), out Probe probe))
                        {
                            ScriptProbe scriptProbe = probe as ScriptProbe;

                            if (scriptProbe?.Agent != null)
                            {
                                string policyJSON = await protocol.RemoteDataStore.GetScriptAgentPolicyAsync(cancellationToken);
                                scriptProbe.Agent.SetPolicy(policyJSON);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Unrecognized push notification command prefix:  " + commandParts.First());
                    }
                }
            }
            catch (Exception pushNotificationCommandException)
            {
                SensusException.Report("Exception while processing push notification command:  " + pushNotificationCommandException.Message, pushNotificationCommandException);
            }
        }

        public async Task DeletePushNotificationRequestAsync(PushNotificationRequest request, CancellationToken cancellationToken)
        {
            // request can be null (e.g., if being called when working with the app-level health test callback, which has no associated protocol)
            if (request == null)
            {
                return;
            }

            await DeletePushNotificationRequestAsync(request.Id, request.Protocol, cancellationToken);
        }

        public async Task DeletePushNotificationRequestAsync(string id, Protocol protocol, CancellationToken cancellationToken)
        {
            // bail if id or protocol are null. we need each of these to attempt the delete and subsequent retries.
            if (id == null)
            {
                SensusException.Report("Received null PNR id to delete.");
                return;
            }

            if (protocol == null)
            {
                SensusException.Report("Received null PNR protocol.");
                return;
            }

            try
            {
                await protocol.RemoteDataStore.DeletePushNotificationRequestAsync(id, cancellationToken);

                lock (_pushNotificationRequestsToDelete)
                {
                    _pushNotificationRequestsToDelete.RemoveAll(idProtocol => idProtocol.Item1 == id);
                }
            }
            catch (Exception deleteException)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while deleting push notification request:  " + deleteException.Message, LoggingLevel.Normal, GetType());

                // hang on to the push notification for deleting in the future, e.g., when internet is restored.
                lock (_pushNotificationRequestsToDelete)
                {
                    if (!_pushNotificationRequestsToDelete.Any(idProtocol => idProtocol.Item1 == id))
                    {
                        _pushNotificationRequestsToDelete.Add(new Tuple<string, Protocol>(id, protocol));
                    }
                }
            }
            finally
            {
                // we just deleted the push notification request, so it doesn't make sense for there to be a pending push notification
                // request to send. remove any pending push notifications to send that match the passed id.
                lock (_pushNotificationRequestsToSend)
                {
                    _pushNotificationRequestsToSend.RemoveAll(pushNotificationRequest => pushNotificationRequest.Id == id);
                }
            }
        }

        public async Task TestHealthAsync(CancellationToken cancellationToken)
        {
            #region send all outstanding push notification requests
            List<PushNotificationRequest> pushNotificationRequestsToSend;
            lock (_pushNotificationRequestsToSend)
            {
                pushNotificationRequestsToSend = _pushNotificationRequestsToSend.ToList();
            }

            SensusServiceHelper.Get().Logger.Log("Sending " + pushNotificationRequestsToSend.Count + " outstanding push notification request(s).", LoggingLevel.Normal, GetType());

            foreach (PushNotificationRequest pushNotificationRequestToSend in pushNotificationRequestsToSend)
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
            List<Tuple<string, Protocol>> pushNotificationRequestsToDelete;
            lock (_pushNotificationRequestsToDelete)
            {
                pushNotificationRequestsToDelete = _pushNotificationRequestsToDelete.ToList();
            }

            SensusServiceHelper.Get().Logger.Log("Deleting " + pushNotificationRequestsToDelete.Count + " outstanding push notification request(s).", LoggingLevel.Normal, GetType());

            foreach (Tuple<string, Protocol> pushNotificationRequestIdProtocol in pushNotificationRequestsToDelete)
            {
                try
                {
                    await DeletePushNotificationRequestAsync(pushNotificationRequestIdProtocol.Item1, pushNotificationRequestIdProtocol.Item2, cancellationToken);
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
        }
    }
}