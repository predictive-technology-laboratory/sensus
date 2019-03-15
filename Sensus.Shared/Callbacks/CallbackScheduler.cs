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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AppCenter.Analytics;
using Sensus.Context;
using Sensus.Exceptions;
using Sensus.Extensions;
using System.Linq;

#if __IOS__
using Sensus.Notifications;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
#endif

namespace Sensus.Callbacks
{
    /// <summary>
    /// Sensus schedules operations via a scheduler.
    /// </summary>
    public abstract class CallbackScheduler
    {
        public const string SENSUS_CALLBACK_KEY = "SENSUS-CALLBACK";
        public const string SENSUS_CALLBACK_INVOCATION_ID_KEY = "SENSUS-CALLBACK-INVOCATION-ID";

        private ConcurrentDictionary<string, ScheduledCallback> _idCallback;

        public CallbackScheduler()
        {
            _idCallback = new ConcurrentDictionary<string, ScheduledCallback>();
        }

        protected abstract Task RequestLocalInvocationAsync(ScheduledCallback callback);
        protected abstract void CancelLocalInvocation(ScheduledCallback callback);

        public async Task<ScheduledCallbackState> ScheduleCallbackAsync(ScheduledCallback callback)
        {
            // the next execution time is computed from the time the current method is called, as the
            // caller may hang on to the ScheduledCallback for some time before calling the current method.
            // we set the time here, before adding it to the collection below, so that any callback
            // in the collection will certainly have a next execution time.
            callback.NextExecution = DateTime.Now + callback.Delay;

            if (callback.State != ScheduledCallbackState.Created)
            {
                SensusException.Report("Attemped to schedule callback " + callback.Id + ", which is in the " + callback.State + " state and not the " + ScheduledCallbackState.Created + " state.");
                callback.State = ScheduledCallbackState.Unknown;
            }
            else if (_idCallback.TryAdd(callback.Id, callback))
            {
                callback.InvocationId = Guid.NewGuid().ToString();

                BatchNextExecutionWithToleratedDelay(callback);

                // the state needs to be updated after batching is performed, so that other callbacks don't
                // attempt to batch with it, but before the invocations are requested, so that if the invocation
                // comes back immediately (e.g., being scheduled in the past) the callback is scheduled and 
                // ready to run.
                callback.State = ScheduledCallbackState.Scheduled;

                await RequestLocalInvocationAsync(callback);

#if __IOS__
                await RequestRemoteInvocationAsync(callback);
#endif
            }
            else
            {
                SensusServiceHelper.Get().Logger.Log("Attempted to schedule duplicate callback for " + callback.Id + ".", LoggingLevel.Normal, GetType());
            }

            return callback.State;
        }

        /// <summary>
        /// Batches the <see cref="ScheduledCallback.NextExecution"/> value within the parameters of toleration (<see cref="ScheduledCallback.DelayToleranceBefore"/>
        /// and <see cref="ScheduledCallback.DelayToleranceAfter"/>), given the <see cref="ScheduledCallback"/>s that are already scheduled to run.
        /// </summary>
        /// <param name="callback">Callback.</param>
        private void BatchNextExecutionWithToleratedDelay(ScheduledCallback callback)
        {
            callback.Batched = false;

            // if delay tolerance is allowed, look for other scheduled callbacks in range of the delay tolerance.
            if (callback.DelayToleranceTotal.Ticks > 0)
            {
                DateTime rangeStart = callback.NextExecution.Value - callback.DelayToleranceBefore;
                DateTime rangeEnd = callback.NextExecution.Value + callback.DelayToleranceAfter;

                ScheduledCallback closestCallbackInRange = _idCallback.Values.Where(existingCallback => existingCallback != callback &&                              // the current callback will already have been added to the collection. don't consider it.
                                                                                                        existingCallback.NextExecution.Value >= rangeStart &&        // consider callbacks within range of the current
                                                                                                        existingCallback.NextExecution.Value <= rangeEnd &&          // consider callbacks within range of the current
                                                                                                        !existingCallback.Batched &&                                 // don't consider batching with other callbacks that are themselves batched, as this can potentially create batch cycling if the delay tolerance values are large.
                                                                                                        existingCallback.State == ScheduledCallbackState.Scheduled)  // consider callbacks that are already scheduled. we don't want to batch with callbacks that are, e.g., running or recently completed.

                                                                             // get existing callback with execution time closest to the current callback's time
                                                                             .OrderBy(existingCallback => Math.Abs(callback.NextExecution.Value.Ticks - existingCallback.NextExecution.Value.Ticks))

                                                                             // there might not be a callback within range
                                                                             .FirstOrDefault();
                // use the closest if there is one in range
                if (closestCallbackInRange != null)
                {
                    SensusServiceHelper.Get().Logger.Log("Batching callback " + callback.Id + ":" + Environment.NewLine +
                                                         "\tCurrent time:  " + callback.NextExecution + Environment.NewLine +
                                                         "\tRange:  " + rangeStart + " -- " + rangeEnd + Environment.NewLine +
                                                         "\tNearest:  " + closestCallbackInRange.Id + Environment.NewLine +
                                                         "\tNew time:  " + closestCallbackInRange.NextExecution, LoggingLevel.Normal, GetType());

                    callback.NextExecution = closestCallbackInRange.NextExecution;
                    callback.Batched = true;
                }
            }
        }

        public bool ContainsCallback(ScheduledCallback callback)
        {
            if (callback == null)
            {
                SensusException.Report("Attempted to check contains of null callback.");
                return false;
            }
            // we should never get a null callback id, but it seems that we are from android.
            else if (callback.Id == null)
            {
                SensusException.Report("Attempted to check contains of callback that has null id.");
                return false;
            }
            else
            {
                return _idCallback.ContainsKey(callback.Id);
            }
        }

        protected ScheduledCallback TryGetCallback(string id)
        {
            ScheduledCallback callback;
            _idCallback.TryGetValue(id, out callback);
            return callback;
        }

        public async Task ServiceCallbackFromPushNotificationAsync(string callbackId, string invocationId, CancellationToken cancellationToken)
        {
            SensusServiceHelper serviceHelper = SensusServiceHelper.Get();

            // it is conceivable that a push notification could arrive in the absence of a running
            // app. in this case, the service helper would be null and there is nothing to do.
            if (serviceHelper != null)
            {
                ScheduledCallback callback = TryGetCallback(callbackId);

                // callback might have been unscheduled
                if (callback != null)
                {
                    SensusServiceHelper.Get().Logger.Log("Attempting to service callback " + callback.Id + " from push notification.", LoggingLevel.Normal, GetType());

                    // if the cancellation token is cancelled, cancel the callback
                    cancellationToken.Register(() =>
                    {
                        CancelRaisedCallback(callback);
                    });

                    await ServiceCallbackAsync(callback, invocationId);
                }
            }
        }

        public abstract Task ServiceCallbackAsync(ScheduledCallback callback, string invocationId);

        /// <summary>
        /// Raises a callback. This involves initiating the callback, setting up cancellation timing for the callback's actions, and scheduling the next
        /// invocation of the callback in the case of repeating callbacks.
        /// </summary>
        /// <returns>Async task</returns>
        /// <param name="callback">Callback to raise.</param>
        /// <param name="invocationId">Identifier of invocation.</param>
        /// <param name="notifyUser">If set to <c>true</c>, then notify user that the callback is being raised.</param>
        public virtual async Task RaiseCallbackAsync(ScheduledCallback callback, string invocationId, bool notifyUser)
        {
            try
            {
                if (callback == null)
                {
                    throw new NullReferenceException("Attemped to raise null callback.");
                }

                // the same callback must not be run multiple times concurrently, so drop the current callback if it's already running. multiple
                // callers might compete for the same callback, but only one will win the lock below and it will exclude all others until the
                // the callback has finished executing. furthermore, the callback must not run multiple times in sequence (e.g., if the callback
                // is raised by the local scheduling system and then later by a remote push notification). this is handled by tracking invocation
                // identifiers, which are only runnable once.
                string initiationError = null;
                lock (callback)
                {
                    if (callback.State != ScheduledCallbackState.Scheduled)
                    {
                        initiationError += "Callback " + callback.Id + " is not scheduled. Current state:  " + callback.State;
                    }

                    if (invocationId != callback.InvocationId)
                    {
                        initiationError += (initiationError == null ? "" : ". ") + "Invocation ID provided for callback " + callback.Id + " does not match the one on record.";
                    }

                    if (initiationError == null)
                    {
                        callback.State = ScheduledCallbackState.Running;
                    }
                }

                if (initiationError == null)
                {
                    try
                    {
                        if (callback.Canceller.IsCancellationRequested)
                        {
                            SensusServiceHelper.Get().Logger.Log("Callback " + callback.Id + " was cancelled before it was raised.", LoggingLevel.Normal, GetType());
                        }
                        else
                        {
                            SensusServiceHelper.Get().Logger.Log("Raising callback " + callback.Id + ".", LoggingLevel.Normal, GetType());

                            if (notifyUser)
                            {
                                await SensusContext.Current.Notifier.IssueNotificationAsync("Sensus", callback.UserNotificationMessage, callback.Id, true, callback.Protocol, null, callback.DisplayPage);
                            }

                            // if the callback specified a timeout, request cancellation at the specified time.
                            if (callback.Timeout.HasValue)
                            {
                                callback.Canceller.CancelAfter(callback.Timeout.Value);
                            }

                            await callback.ActionAsync(callback.Canceller.Token);
                        }
                    }
                    catch (Exception raiseException)
                    {
                        SensusException.Report("Callback " + callback.Id + " threw an exception:  " + raiseException.Message, raiseException);
                    }
                    finally
                    {
                        // the cancellation token source for the current callback might have been canceled. if this is a repeating callback then we'll need a new
                        // cancellation token source because they cannot be reset and we're going to use the same scheduled callback again for the next repeat. 
                        // if we enter the _idCallback lock before CancelRaisedCallback does, then the next raise will be cancelled. if CancelRaisedCallback enters the 
                        // _idCallback lock first, then the cancellation token source will be overwritten here and the cancel will not have any effect on the next 
                        // raise. the latter case is a reasonable outcome, since the purpose of CancelRaisedCallback is to terminate a callback that is currently in 
                        // progress, and the current callback is no longer in progress. if the desired outcome is complete discontinuation of the repeating callback
                        // then UnscheduleRepeatingCallback should be used -- this method first cancels any raised callbacks and then removes the callback entirely.
                        try
                        {
                            if (callback.RepeatDelay.HasValue)
                            {
                                callback.Canceller = new CancellationTokenSource();
                            }
                        }
                        catch (Exception ex)
                        {
                            SensusException.Report("Exception while assigning new callback canceller.", ex);
                        }
                        finally
                        {
                            callback.State = ScheduledCallbackState.Completed;

                            // schedule callback again if it is still scheduled with a valid repeat delay
                            if (ContainsCallback(callback) &&
                                callback.RepeatDelay.HasValue &&
                                callback.RepeatDelay.Value.Ticks > 0)
                            {
                                callback.NextExecution = DateTime.Now + callback.RepeatDelay.Value;
                                callback.InvocationId = Guid.NewGuid().ToString();  // set the new invocation ID before resetting the state so that concurrent callers won't run (their invocation IDs won't match)

                                BatchNextExecutionWithToleratedDelay(callback);

                                // the state needs to be updated after batching is performed, so that other callbacks don't
                                // attempt to batch with it, but before the invocations are requested, so that if the invocation
                                // comes back immediately (e.g., being scheduled in the past) the callback is scheduled and 
                                // ready to run.
                                callback.State = ScheduledCallbackState.Scheduled;

                                await RequestLocalInvocationAsync(callback);

#if __IOS__
                                await RequestRemoteInvocationAsync(callback);
#endif
                            }
                            else
                            {
                                await UnscheduleCallbackAsync(callback);
                            }
                        }
                    }
                }
                else
                {
                    SensusServiceHelper.Get().Logger.Log("Initiation error for callback " + callback.Id + ":  " + initiationError, LoggingLevel.Normal, GetType());
                }
            }
            catch (Exception ex)
            {
                SensusException.Report("Exception raising callback:  " + ex.Message, ex);
            }
        }

#if __IOS__
        /// <summary>
        /// Requests remote invocation for a <see cref="ScheduledCallback"/>, to be delivered in parallel with the 
        /// local invocation loop on iOS. Only one of these (the remote or local) will ultimately be allowed to 
        /// run -- whichever arrives first.
        /// </summary>
        /// <returns>Task.</returns>
        /// <param name="callback">Callback.</param>
        private async Task RequestRemoteInvocationAsync(ScheduledCallback callback)
        {
            // not all callbacks are associated with a protocol (e.g., the app-level health test). because push notifications are
            // currently tied to the remote data store of the protocol, we don't currently provide PNR support for such callbacks.
            // on race conditions, it might be the case that the system attempts to schedule a duplicate callback. if this happens
            // the duplicate will not be assigned a next execution, and the system will try to unschedule/delete it. skip such
            // callbacks below.
            if (callback.Protocol != null && callback.NextExecution.HasValue)
            {
                try
                {
                    // the request id must differentiate the current device. furthermore, it needs to identify the
                    // request as one for a callback. lastly, it needs to identify the particular callback that it
                    // targets. the id does not include the callback invocation, as any newer requests for the 
                    // callback should obsolete older requests.
                    string id = SensusServiceHelper.Get().DeviceId + "." + SENSUS_CALLBACK_KEY + "." + callback.Id;

                    PushNotificationUpdate update = new PushNotificationUpdate
                    {
                        Type = PushNotificationUpdateType.Callback,
                        Content = JObject.Parse("{" +
                                                    "\"callback-id\":" + JsonConvert.ToString(callback.Id) + "," +
                                                    "\"invocation-id\":" + JsonConvert.ToString(callback.InvocationId) +
                                                "}")
                    };

                    PushNotificationRequest request = new PushNotificationRequest(id, SensusServiceHelper.Get().DeviceId, callback.Protocol, update, PushNotificationRequest.LocalFormat, callback.NextExecution.Value, callback.PushNotificationBackendKey);

                    await SensusContext.Current.Notifier.SendPushNotificationRequestAsync(request, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    SensusException.Report("Exception while sending push notification request for scheduled callback:  " + ex.Message, ex);
                }
            }
        }

        private async Task CancelRemoteInvocationAsync(ScheduledCallback callback)
        {            
            await SensusContext.Current.Notifier.DeletePushNotificationRequestAsync(callback.PushNotificationBackendKey, callback.Protocol, CancellationToken.None);
        }
#endif

        public void TestHealth()
        {
            foreach (ScheduledCallback callback in _idCallback.Values)
            {
                // the following states should be extremely short-lived. if they are present
                // then something has likely gone wrong. track the event.
                if (callback.State == ScheduledCallbackState.Created ||
                    callback.State == ScheduledCallbackState.Completed)
                {
                    string eventName = TrackedEvent.Warning + ":" + GetType().Name;
                    Dictionary<string, string> properties = new Dictionary<string, string>
                    {
                        { "Callback State", callback.State + ":" + callback.Id }
                    };

                    Analytics.TrackEvent(eventName, properties);
                }
                else if (callback.State == ScheduledCallbackState.Scheduled)
                {
                    // if the callback is scheduled and has a next execution time, check for latency.
                    if (callback.NextExecution.HasValue)
                    {
                        TimeSpan latency = DateTime.Now - callback.NextExecution.Value;

                        if (latency.TotalMinutes > 1)
                        {
                            string eventName = TrackedEvent.Warning + ":" + GetType().Name;
                            Dictionary<string, string> properties = new Dictionary<string, string>
                            {
                                { "Callback Latency", latency.TotalMinutes.RoundToWhole(5) + ":" + callback.Id }
                            };

                            Analytics.TrackEvent(eventName, properties);
                        }
                    }
                    else
                    {
                        // report missing next execution time
                        string eventName = TrackedEvent.Warning + ":" + GetType().Name;
                        Dictionary<string, string> properties = new Dictionary<string, string>
                        {
                            { "Callback Next Execution", "NONE" }
                        };

                        Analytics.TrackEvent(eventName, properties);
                    }
                }
            }
        }

        /// <summary>
        /// Cancels a callback that has been raised and is currently executing.
        /// </summary>
        /// <param name="callback">Callback.</param>
        public void CancelRaisedCallback(ScheduledCallback callback)
        {
            callback.Canceller.Cancel();
            SensusServiceHelper.Get().Logger.Log("Cancelled callback " + callback.Id + ".", LoggingLevel.Normal, GetType());
        }

        /// <summary>
        /// Unschedules the callback, first cancelling any executions that are currently running and then removing the callback from the scheduler.
        /// </summary>
        /// <param name="callback">Callback.</param>
        public async Task UnscheduleCallbackAsync(ScheduledCallback callback)
        {
            if (callback != null)
            {
                SensusServiceHelper.Get().Logger.Log("Unscheduling callback " + callback.Id + ".", LoggingLevel.Normal, GetType());

                // interrupt any current executions
                CancelRaisedCallback(callback);

                // remove from the scheduler
                _idCallback.TryRemove(callback.Id, out ScheduledCallback removedCallback);

                CancelLocalInvocation(callback);

#if __IOS__
                await CancelRemoteInvocationAsync(callback);
#else
                await Task.CompletedTask;
#endif

                SensusServiceHelper.Get().Logger.Log("Unscheduled callback " + callback.Id + ".", LoggingLevel.Normal, GetType());
            }
        }
    }
}