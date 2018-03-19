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
using System.Threading;
using System.Threading.Tasks;
using Sensus.Context;
using Sensus.Exceptions;

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

        public bool ScheduleCallback(ScheduledCallback callback)
        {
            if (!_idCallback.TryAdd(callback.Id, callback))
            {
                return false;
            }

            callback.NextExecution = DateTime.Now + callback.Delay;

            ScheduleCallbackPlatformSpecific(callback);

            return true;
        }

        public bool CallbackIsScheduled(ScheduledCallback callback)
        {
            // we should never get a null callback id, but it seems that we are from android.
            if (callback?.Id == null)
            {
                SensusException.Report("Received null callback id.");
                return false;
            }
            else
            {
                return _idCallback.ContainsKey(callback.Id);
            }
        }

        public ScheduledCallback TryGetCallback(string id)
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
            DateTime callbackStartTime = DateTime.Now;

            return Task.Run(async () =>
            {
                try
                {
                    if (callback != null)
                    {
                        // the same callback action cannot be run multiple times concurrently. drop the current callback if it's already running. multiple
                        // callers might compete for the same callback, but only one will win the lock below and it will exclude all others until it has executed.
                        bool actionAlreadyRunning = true;
                        lock (callback)
                        {
                            if (!callback.Running)
                            {
                                actionAlreadyRunning = false;
                                callback.Running = true;
                            }
                        }

                        if (actionAlreadyRunning)
                        {
                            SensusServiceHelper.Get().Logger.Log("Callback " + callback.Id + " is already running. Not running again.", LoggingLevel.Normal, GetType());
                        }
                        else
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
                                string errorMessage = "Callback " + callback.Id + " threw an exception:  " + raiseException.Message;
                                SensusServiceHelper.Get().Logger.Log(errorMessage, LoggingLevel.Normal, GetType());
                                SensusException.Report(errorMessage, raiseException);
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
                                        lock (_idCallback)
                                        {
                                            callback.Canceller = new CancellationTokenSource();
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                }
                                finally
                                {
                                    // if we marked the callback as running, ensure that we unmark it (note we're nested within two finally blocks so
                                    // this will always execute). this will allow the callback to run again.
                                    lock (callback)
                                    {
                                        callback.Running = false;
                                    }

                                    // schedule callback again if it is still scheduled with a valid repeat delay
                                    if (CallbackIsScheduled(callback) &&
                                        callback.RepeatDelay.HasValue && 
                                        callback.RepeatDelay.Value.Ticks > 0 &&
                                        scheduleRepeatCallback != null)
                                    {
                                        DateTime nextCallbackTime;

                                        // if this repeating callback is allowed to lag, schedule the repeat from the current time.
                                        if (callback.AllowRepeatLag.Value)
                                        {
                                            nextCallbackTime = DateTime.Now + callback.RepeatDelay.Value;
                                        }
                                        else
                                        {
                                            // otherwise, schedule the repeat from the time at which the current callback was raised.
                                            nextCallbackTime = callbackStartTime + callback.RepeatDelay.Value;
                                        }

                                        callback.NextExecution = nextCallbackTime;

                                        scheduleRepeatCallback();
                                    }
                                    else
                                    {
                                        UnscheduleCallback(callback);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    SensusException.Report("Failed to raise callback:  " + ex.Message, ex);
                }
            });
        }

        /// <summary>
        /// Cancels a callback that has been raised and is currently executing.
        /// </summary>
        /// <param name="callback">Callback.</param>
        public void CancelRaisedCallback(ScheduledCallback callback)
        {
            callback?.Canceller.Cancel();
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
            }
        }
    }
}