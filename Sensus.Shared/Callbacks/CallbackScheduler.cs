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

namespace Sensus.Callbacks
{
    /// <summary>
    /// Sensus schedules operations via a scheduler.
    /// </summary>
    public abstract class CallbackScheduler : ICallbackScheduler
    {
        public const string SENSUS_CALLBACK_KEY = "SENSUS-CALLBACK";

        private ConcurrentDictionary<string, ScheduledCallback> _idCallback;

        public CallbackScheduler()
        {
            _idCallback = new ConcurrentDictionary<string, ScheduledCallback>();
        }

        protected abstract void ScheduleCallbackPlatformSpecific(ScheduledCallback callback);
        protected abstract void UnscheduleCallbackPlatformSpecific(ScheduledCallback callback);

        public ScheduledCallbackState ScheduleCallback(ScheduledCallback callback)
        {
            if (callback.State != ScheduledCallbackState.Created)
            {
                SensusException.Report("Attemped to schedule callback " + callback.Id + ", which is in the " + callback.State + " state and not the " + ScheduledCallbackState.Created + " state.");
                callback.State = ScheduledCallbackState.Unknown;
            }
            else if (_idCallback.TryAdd(callback.Id, callback))
            {
                callback.NextExecution = DateTime.Now + callback.Delay;
                callback.State = ScheduledCallbackState.Scheduled;

                ScheduleCallbackPlatformSpecific(callback);
            }
            else
            {
                SensusException.Report("Attempted to schedule duplicate callback for " + callback.Id + ".");
            }

            return callback.State;
        }

        public bool ContainsCallback(ScheduledCallback callback)
        {
            // we should never get a null callback id, but it seems that we are from android.
            if (callback?.Id == null)
            {
                SensusException.Report("Attempted to check scheduling status of callback with null id.");
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

            if (callback == null)
            {
                SensusException.Report("Failed to retrieve callback " + id + ".");
            }

            return callback;
        }

        /// <summary>
        /// Raises a callback.
        /// </summary>
        /// <returns>Async task</returns>
        /// <param name="callback">Callback to raise.</param>
        /// <param name="notifyUser">If set to <c>true</c>, then notify user that the callback is being raised.</param>
        /// <param name="scheduleRepeatCallback">Platform-specific action to execute to schedule the next execution of the callback.</param>
        /// <param name="letDeviceSleepCallback">Action to execute when the system should be allowed to sleep.</param>
        public virtual Task RaiseCallbackAsync(ScheduledCallback callback, bool notifyUser, Action scheduleRepeatCallback, Action letDeviceSleepCallback)
        {
            return Task.Run(async () =>
            {
                try
                {
                    if (callback == null)
                    {
                        throw new NullReferenceException("Attemped to raise null callback.");
                    }

                    // the same callback cannot be run multiple times concurrently, so drop the current callback if it's already running. multiple
                    // callers might compete for the same callback, but only one will win the lock below and it will exclude all others until the
                    // the callback has finished executing.
                    bool runCallbackNow = false;
                    lock (callback)
                    {
                        if (callback.State == ScheduledCallbackState.Scheduled)
                        {
                            runCallbackNow = true;
                            callback.State = ScheduledCallbackState.Running;
                        }
                    }

                    if (runCallbackNow)
                    {
                        try
                        {
                            if (callback.Canceller.IsCancellationRequested)
                            {
                                throw new Exception("Callback " + callback.Id + " was cancelled before it was raised.");
                            }
                            else
                            {
                                SensusServiceHelper.Get().Logger.Log("Raising callback " + callback.Id + ".", LoggingLevel.Normal, GetType());

                                if (notifyUser)
                                {
                                    SensusContext.Current.Notifier.IssueNotificationAsync("Sensus", callback.UserNotificationMessage, callback.Id, callback.Protocol, true, callback.DisplayPage);
                                }

                                // if the callback specified a timeout, request cancellation at the specified time.
                                if (callback.CallbackTimeout.HasValue)
                                {
                                    callback.Canceller.CancelAfter(callback.CallbackTimeout.Value);
                                }

                                await callback.Action(callback.Id, callback.Canceller.Token, letDeviceSleepCallback);
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
                                    callback.RepeatDelay.Value.Ticks > 0 &&
                                    scheduleRepeatCallback != null)
                                {
                                    // if this repeating callback is allowed to lag, schedule the next execution from the current time.
                                    if (callback.AllowRepeatLag.Value)
                                    {
                                        callback.NextExecution = DateTime.Now + callback.RepeatDelay.Value;
                                    }
                                    else
                                    {
                                        // otherwise, schedule the next execution from the time at which the current callback was supposed to be raised.
                                        callback.NextExecution = callback.NextExecution.Value + callback.RepeatDelay.Value;

                                        // if we've lagged so long that the next execution is already in the past, just reschedule for now. this will cause
                                        // the rescheduled callback to be raised as soon as possible, subject to delays in the systems scheduler (e.g., on
                                        // android most alarms do not come back immediately, even if requested).
                                        if (callback.NextExecution.Value < DateTime.Now)
                                        {
                                            callback.NextExecution = DateTime.Now;
                                        }
                                    }

                                    callback.State = ScheduledCallbackState.Scheduled;

                                    scheduleRepeatCallback();
                                }
                                else
                                {
                                    UnscheduleCallback(callback);
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Callback " + callback.Id + " was already running. Not running again.");
                    }
                }
                catch (Exception ex)
                {
                    SensusException.Report("Failed to raise callback:  " + ex.Message, ex);
                }
            });
        }

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
        public void UnscheduleCallback(ScheduledCallback callback)
        {
            if (callback != null)
            {
                SensusServiceHelper.Get().Logger.Log("Unscheduling callback " + callback.Id + ".", LoggingLevel.Normal, GetType());

                // interrupt any current executions
                CancelRaisedCallback(callback);

                // remove from the scheduler
                ScheduledCallback removedCallback;
                _idCallback.TryRemove(callback.Id, out removedCallback);

                // tell the current platform cancel its hook into the system's callback system
                UnscheduleCallbackPlatformSpecific(callback);

                SensusServiceHelper.Get().Logger.Log("Unscheduled callback " + callback.Id + ".", LoggingLevel.Normal, GetType());
            }
        }
    }
}