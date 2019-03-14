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
using Sensus.Probes;
using System.Reflection;
using Newtonsoft.Json;
using Sensus.Extensions;
using Newtonsoft.Json.Linq;

namespace Sensus.Notifications
{
    /// <summary>
    /// Exposes the user-facing notification functionality of a platform. Also manages push notifications.
    /// </summary>
    public abstract class Notifier
    {
        public const string PENDING_SURVEY_TEXT_NOTIFICATION_ID = "SENSUS-PENDING-SURVEY-TEXT-NOTIFICATION";
        public const string PENDING_SURVEY_BADGE_NOTIFICATION_ID = "SENSUS-PENDING-SURVEY-BADGE-NOTIFICATION";
        public const string DISPLAY_PAGE_KEY = "SENSUS-DISPLAY-PAGE";

        private List<PushNotificationRequest> _pushNotificationRequestsToSend;

        /// <summary>
        /// When trying to delete a push notification request, we don't always have the
        /// original <see cref="PushNotificationRequest"/> object. This happens, for example,
        /// when we receive the push notification. All we have in that case is the backend key and 
        /// protocol. So just track this information. Furthermore, we used to keep an object
        /// reference to the <see cref="Protocol"/>, but this caused problems when <see cref="Protocol"/>s
        /// are replaced upon loading. Instead of attempting to update the object references 
        /// in this collection, simply track the identifier and grab the current <see cref="Protocol"/>
        /// when needed.
        /// </summary>
        private List<Tuple<Guid, string>> _pushNotificationBackendKeysProtocolIdsToDelete;

        public Notifier()
        {
            _pushNotificationRequestsToSend = new List<PushNotificationRequest>();
            _pushNotificationBackendKeysProtocolIdsToDelete = new List<Tuple<Guid, string>>();
        }

        public abstract Task IssueNotificationAsync(string title, string message, string id, bool alertUser, Protocol protocol, int? badgeNumber, DisplayPage displayPage);

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

            // if the PNR targets the current device but the protocol isn't listening, then don't send the request. this 
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

                RemovePushNotificationRequestToSend(request.BackendKey);
            }
            catch (Exception sendException)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while sending push notification request:  " + sendException.Message, LoggingLevel.Normal, GetType());

                AddPushNotificationRequestToSend(request);
            }
            finally
            {
                // we just attempted to send the push notification request, so it does not make sense for the
                // request to be pending deletion. remove any pending push notification deletions.
                RemovePushNotificationRequestToDelete(request.BackendKey);
            }
        }

        public async Task ProcessReceivedPushNotificationAsync(PushNotification pushNotification, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(pushNotification.Id))
            {
                throw new Exception("Push notification ID is missing or blank.");
            }

            SensusServiceHelper.Get().Logger.Log("Processing push notification " + pushNotification.Id, LoggingLevel.Normal, GetType());

            Protocol protocol = pushNotification.GetProtocol();

            // it's possible for the user to delete the protocol but to continue receiving push notifications. ignore them.
            // we won't be able to delete the push notification request below, and so we'll likely continue to receive
            // the notification from the backend until it expires.
            if (protocol == null)
            {
                return;
            }

            // delete the push notification request from the backend if we have the key. it used to be that the backend deleted 
            // push notification requests after they were delivered; however, this isn't a good idea because deliveries sometimes 
            // fail...maybe the device has no internet connection, maybe the push service is down or fails, etc. in such cases, 
            // it used to be that the push notification would never be retried. by waiting for the push notification to arrive 
            // and having the app delete the request, we ensure that any such failures will cause the push notification to be retried.
            try
            {
                if (pushNotification.BackendKey != null)
                {
                    await DeletePushNotificationRequestAsync(pushNotification.BackendKey.Value, protocol, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // report the failure but do not return from the method. we can still hopefully execute the
                // push notification request. the backend will eventually retry the push notification, which
                // should be ignored by the app or filtered out by the backend prior to delivery.
                SensusException.Report("Failed to delete push notification from backend:  " + ex.Message, ex);
            }

            // check if the targeted protocol is stopped but should be running. this can happen if the app dies while the 
            // protocol is running (e.g., due to system killing it or an exception) and is subsequently restarted (e.g., 
            // due to resource pressure alleviation or the arrival of a push notification). attempt to start the protocol.
            if (protocol.State == ProtocolState.Stopped && SensusServiceHelper.Get().RunningProtocolIds.Contains(protocol.Id))
            {
                SensusServiceHelper.Get().Logger.Log("Push notification targets a protocol that is stopped but should be running. Starting protocol.", LoggingLevel.Normal, GetType());

#if __IOS__
                // starting the protocol can be time consuming and run afoul of ios push notification processing 
                // constraints. start a background task and let the time consuming aspects of app startup 
                // (e.g., scheduling callbacks for script runs) take care of monitoring the background time remaining.
                nint protocolStartTaskId = -1;
                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                {
                    SensusServiceHelper.Get().Logger.Log("Starting background task for protocol start on push notification.", LoggingLevel.Normal, GetType());

                    protocolStartTaskId = UIKit.UIApplication.SharedApplication.BeginBackgroundTask(() =>
                    {
                        // can't think of anything to do if we run out of time. report the error.
                        SensusException.Report("Ran out of background time when starting protocol for push notification.");
                    });
                });
#endif

                // all is lost if we cannot start the protocol. so don't pass the cancellation token to StartAsync.
                await protocol.StartAsync(CancellationToken.None);

#if __IOS__
                // end the ios background task.
                SensusContext.Current.MainThreadSynchronizer.ExecuteThreadSafe(() =>
                {
                    UIKit.UIApplication.SharedApplication.EndBackgroundTask(protocolStartTaskId);

                    SensusServiceHelper.Get().Logger.Log("Ended background task for protocol start on push notification.", LoggingLevel.Normal, GetType());
                });
#endif
            }

            // if the protocol is not running and never will be, then we have nothing to do. we explicitly attempt to 
            // prevent such notifications from coming through by unregistering from hubs that lack running/scheduled 
            // protocols and deleting the token from the backend; however, there may be race conditions (e.g., stopping 
            // the protocol just before the arrival of a push notification, or receiving an old push notification while
            // the protocol is starting up) that allow a push notification to be delivered to us nonetheless. if the 
            // protocol is not running but is scheduled to start in the future, then allow it as the push notification 
            // could be the start command itself.
            if (protocol.State != ProtocolState.Running && !protocol.StartIsScheduled)
            {
                SensusServiceHelper.Get().Logger.Log("Protocol targeted by push notification is not currently running and is not scheduled to run.", LoggingLevel.Normal, GetType());
                return;
            }

            if (pushNotification.Update)
            {
                List<PushNotificationUpdate> updates = null;
                try
                {
                    updates = await protocol.RemoteDataStore.GetUpdatesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while getting push notification updates:  " + ex.Message, LoggingLevel.Normal, GetType());
                }

                foreach (PushNotificationUpdate update in updates)
                {
                    try
                    {
                        if (update.Type == PushNotificationUpdateType.Callback)
                        {
                            string callbackId = update.Content.Value<string>("callback-id");
                            string invocationId = update.Content.Value<string>("invocation-id");

#if __IOS__
                            // cancel any previously delivered local notifications for the callback. we do not need to cancel
                            // any pending notifications, as they will either (a) be canceled if the callback is non-repeating
                            // or (b) be replaced if the callback is repeating and gets rescheduled. furthermore, there is a 
                            // race condition on app activation in which the callback is updated, run, and rescheduled, after
                            // which the push notification is delivered and is processed. cancelling the newly rescheduled
                            // pending local push notification at this point will terminate the local invocation loop, and the 
                            // callback command at this point will contain an invalid invocation ID causing it to not be 
                            // rescheduled). thus, both local and remote invocation will terminate and the probe will halt.
                            UserNotifications.UNUserNotificationCenter.Current.RemoveDeliveredNotifications(new[] { callbackId });
#endif

                            await SensusContext.Current.CallbackScheduler.ServiceCallbackFromPushNotificationAsync(callbackId, invocationId, cancellationToken);
                        }
                        else if (update.Type == PushNotificationUpdateType.Protocol)
                        {
                            bool restartProtocol = false;
                            List<Probe> updatedProbesToRestart = new List<Probe>();
                            foreach (JObject updateObject in update.Content.Value<JArray>("updates"))
                            {
                                // catch any exceptions so that we process all updates
                                try
                                {
                                    string propertyTypeName = updateObject.Value<string>("property-type");
                                    string propertyName = updateObject.Value<string>("property-name");
                                    string targetTypeName = updateObject.Value<string>("target-type");
                                    string newValueString = updateObject.Value<string>("value");

                                    // get property type
                                    Type propertyType;
                                    try
                                    {
                                        propertyType = Assembly.GetExecutingAssembly().GetType(propertyTypeName, true);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception("Exception while getting property type (" + propertyTypeName + "):  " + ex.Message, ex);
                                    }

                                    // get property
                                    PropertyInfo property = propertyType.GetProperty(propertyName);

                                    // get target type
                                    Type targetType;
                                    try
                                    {
                                        targetType = Assembly.GetExecutingAssembly().GetType(targetTypeName, true);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception("Exception while getting target type (" + targetTypeName + "):  " + ex.Message, ex);
                                    }

                                    // if the value is JSON, then assume it is a reference type.
                                    object newValueObject = null;
                                    if (newValueString.IsValidJsonObject())
                                    {
                                        newValueObject = JsonConvert.DeserializeObject(newValueString);
                                    }
                                    // otherwise, assume it is a value type.
                                    else
                                    {
                                        // watch out for nullable value types when converting the string to its value type
                                        Type baseType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                                        newValueObject = Convert.ChangeType(newValueString, baseType);
                                    }

                                    // update the protocol and request restart
                                    if (targetType == typeof(Protocol))
                                    {
                                        property.SetValue(protocol, newValueObject);

                                        // restart the protocol if it is starting, running, or paused (other than stopping or stopped)
                                        if (protocol.State == ProtocolState.Starting ||
                                            protocol.State == ProtocolState.Running ||
                                            protocol.State == ProtocolState.Paused)
                                        {
                                            restartProtocol = true;
                                        }
                                    }
                                    else if (targetType.GetAncestorTypes(false).Last() == typeof(Probe))
                                    {
                                        // update each probe derived from the target type
                                        foreach (Probe probe in protocol.Probes)
                                        {
                                            if (probe.GetType().GetAncestorTypes(false).Any(ancestorType => ancestorType == targetType))
                                            {
                                                // don't set the new value if it matches the current value
                                                object currentValueObject = property.GetValue(probe);
                                                if (newValueObject.Equals(currentValueObject))
                                                {
                                                    SensusServiceHelper.Get().Logger.Log("Current and new values match. Not updating probe.", LoggingLevel.Normal, GetType());
                                                }
                                                else
                                                {
                                                    property.SetValue(probe, newValueObject);

                                                    if (probe.Running || probe.Enabled)
                                                    {
                                                        if (!updatedProbesToRestart.Contains(probe))
                                                        {
                                                            updatedProbesToRestart.Add(probe);
                                                        }
                                                    }

                                                    // record the update as a datum in the data store, so that we can analyze results of updates retrospectively.
                                                    protocol.LocalDataStore.WriteDatum(new ProtocolUpdateDatum(DateTimeOffset.UtcNow, propertyTypeName, propertyName, targetTypeName, newValueString), cancellationToken);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception("Unrecognized update target type:  " + targetType.FullName);
                                    }

                                    SensusServiceHelper.Get().Logger.Log("Updated protocol:  " + propertyTypeName + "." + propertyName + " for each " + targetTypeName + " = " + newValueString, LoggingLevel.Normal, GetType());
                                }
                                catch (Exception updateException)
                                {
                                    SensusServiceHelper.Get().Logger.Log("Exception while processing protocol update:  " + updateException.Message, LoggingLevel.Normal, GetType());
                                }
                            }

                            bool notifyUser = false;

                            // restart the protocol if needed. this will have the side-effect of restarting all probes and saving the app state.
                            if (restartProtocol)
                            {
                                await protocol.StopAsync();
                                await protocol.StartAsync(cancellationToken);
                                notifyUser = true;
                            }
                            else
                            {
                                // restart individual probes to take on updated settings
                                SensusServiceHelper.Get().Logger.Log("Restarting " + updatedProbesToRestart.Count + " updated probe(s) following push notification updates.", LoggingLevel.Normal, GetType());
                                bool probeRestarted = false;
                                foreach (Probe probeToRestart in updatedProbesToRestart)
                                {
                                    try
                                    {
                                        await probeToRestart.RestartAsync();
                                        probeRestarted = true;
                                    }
                                    catch (Exception ex)
                                    {
                                        SensusServiceHelper.Get().Logger.Log("Exception while restarting probe following push notification update:  " + ex.Message, LoggingLevel.Normal, GetType());
                                    }
                                }

                                if (probeRestarted)
                                {
                                    await SensusServiceHelper.Get().SaveAsync();
                                    notifyUser = true;
                                }
                            }

                            // let the user know what happened if requested
                            if (notifyUser)
                            {
                                JObject userNotificationObject = update.Content.Value<JObject>("user-notification");

                                if (userNotificationObject != null)
                                {
                                    string message = userNotificationObject.Value<string>("message");
                                    await IssueNotificationAsync("Study Updated", "Your study has been updated" + (string.IsNullOrWhiteSpace(message) ? "." : ":  " + message.Trim()), update.Id.ToString(), true, protocol, null, DisplayPage.None);
                                }
                            }
                        }
                        else if (update.Type == PushNotificationUpdateType.SurveyAgentPolicy)
                        {
                            await protocol.UpdateScriptAgentPolicyAsync(update.Content);
                        }
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Exception while applying update:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                }
            }
            else
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(pushNotification.Title) && !string.IsNullOrWhiteSpace(pushNotification.Body))
                    {
                        await IssueNotificationAsync(pushNotification.Title, pushNotification.Body, pushNotification.Id, !string.IsNullOrWhiteSpace(pushNotification.Sound), protocol, null, DisplayPage.None);
                    }
                }
                catch (Exception ex)
                {
                    SensusException.Report("Exception while notifying from push notification:  " + ex.Message, ex);
                }
            }
        }

        public async Task DeletePushNotificationRequestAsync(Guid backendKey, Protocol protocol, CancellationToken cancellationToken)
        {
            if (protocol == null)
            {
                SensusException.Report("Received null PNR protocol.");
                return;
            }

            try
            {
                await protocol.RemoteDataStore.DeletePushNotificationRequestAsync(backendKey, cancellationToken);

                RemovePushNotificationRequestToDelete(backendKey);
            }
            catch (Exception deleteException)
            {
                SensusServiceHelper.Get().Logger.Log("Exception while deleting push notification request:  " + deleteException.Message, LoggingLevel.Normal, GetType());

                // hang on to the push notification for deleting in the future, e.g., when internet is restored.
                AddPushNotificationRequestToDelete(backendKey, protocol.Id);
            }
            finally
            {
                // we just attempted to delete the push notification request, so it does not make sense for the
                // request to be pending sending. remove any pending push notification sendings.
                RemovePushNotificationRequestToSend(backendKey);
            }
        }

        private void AddPushNotificationRequestToSend(PushNotificationRequest pushNotificationRequest)
        {
            // hang on to the push notification for sending in the future, e.g., when internet is restored.
            lock (_pushNotificationRequestsToSend)
            {
                int currIndex = _pushNotificationRequestsToSend.IndexOf(pushNotificationRequest);

                if (currIndex < 0)
                {
                    _pushNotificationRequestsToSend.Add(pushNotificationRequest);
                }
                else
                {
                    _pushNotificationRequestsToSend[currIndex] = pushNotificationRequest;
                }
            }
        }

        private void RemovePushNotificationRequestToSend(Guid backendKey)
        {
            lock (_pushNotificationRequestsToSend)
            {
                _pushNotificationRequestsToSend.RemoveAll(pushNotificationRequest => pushNotificationRequest.BackendKey == backendKey);
            }
        }

        private void AddPushNotificationRequestToDelete(Guid backendKey, string protocolId)
        {
            lock (_pushNotificationBackendKeysProtocolIdsToDelete)
            {
                if (!_pushNotificationBackendKeysProtocolIdsToDelete.Any(backendKeyProtocolId => backendKeyProtocolId.Item1 == backendKey))
                {
                    _pushNotificationBackendKeysProtocolIdsToDelete.Add(new Tuple<Guid, string>(backendKey, protocolId));
                }
            }
        }

        private void RemovePushNotificationRequestToDelete(Guid backendKey)
        {
            lock (_pushNotificationBackendKeysProtocolIdsToDelete)
            {
                _pushNotificationBackendKeysProtocolIdsToDelete.RemoveAll(backendKeyProtocolId => backendKeyProtocolId.Item1 == backendKey);
            }
        }

        public async Task TestHealthAsync(CancellationToken cancellationToken)
        {
            #region send all outstanding push notification requests
            // gather up requests within lock, as we'll need to await below.
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
            // gather up requests within lock, as we'll need to await below.
            List<Tuple<Guid, Protocol>> pushNotificationBackendKeysProtocolsToDelete = new List<Tuple<Guid, Protocol>>();
            lock (_pushNotificationBackendKeysProtocolIdsToDelete)
            {
                foreach (Tuple<Guid, string> backendKeyProtocolId in _pushNotificationBackendKeysProtocolIdsToDelete)
                {
                    Protocol protocolForPushNotificationRequest = SensusServiceHelper.Get().RegisteredProtocols.SingleOrDefault(protocol => protocol.Id == backendKeyProtocolId.Item2);

                    // it's possible for the protocol associated with the push notification request to be deleted before we get around to deleting the request
                    if (protocolForPushNotificationRequest == null)
                    {
                        SensusServiceHelper.Get().Logger.Log("No protocol found for push notification to delete.", LoggingLevel.Normal, GetType());
                        _pushNotificationBackendKeysProtocolIdsToDelete.Remove(backendKeyProtocolId);
                    }
                    else
                    {
                        pushNotificationBackendKeysProtocolsToDelete.Add(new Tuple<Guid, Protocol>(backendKeyProtocolId.Item1, protocolForPushNotificationRequest));
                    }
                }
            }

            SensusServiceHelper.Get().Logger.Log("Deleting " + pushNotificationBackendKeysProtocolsToDelete.Count + " outstanding push notification request(s).", LoggingLevel.Normal, GetType());

            foreach (Tuple<Guid, Protocol> pushNotificationBackendKeyProtocolToDelete in pushNotificationBackendKeysProtocolsToDelete)
            {
                try
                {
                    await DeletePushNotificationRequestAsync(pushNotificationBackendKeyProtocolToDelete.Item1, pushNotificationBackendKeyProtocolToDelete.Item2, cancellationToken);
                }
                catch (Exception deleteException)
                {
                    SensusServiceHelper.Get().Logger.Log("Exception while deleting push notification request:  " + deleteException.Message, LoggingLevel.Normal, GetType());
                }
            }

            // report remaining PNRs to delete
            lock (_pushNotificationBackendKeysProtocolIdsToDelete)
            {
                string eventName = TrackedEvent.Health + ":" + GetType().Name;
                Dictionary<string, string> properties = new Dictionary<string, string>
                {
                    { "PNRs to Delete", _pushNotificationBackendKeysProtocolIdsToDelete.Count.ToString() }
                };

                Analytics.TrackEvent(eventName, properties);
            }
            #endregion
        }
    }
}