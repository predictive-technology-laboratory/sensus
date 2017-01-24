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
using Xamarin;

namespace Sensus.Callbacks
{
    public abstract class CallbackScheduler : ICallbackScheduler
    {
        public const string SENSUS_CALLBACK_KEY = "SENSUS-CALLBACK";
        public const string SENSUS_CALLBACK_REPEATING_KEY = "SENSUS-CALLBACK-REPEATING";
        public const string SENSUS_CALLBACK_REPEAT_DELAY_KEY = "SENSUS-CALLBACK-REPEAT-DELAY";
        public const string SENSUS_CALLBACK_REPEAT_LAG_KEY = "SENSUS-CALLBACK-REPEAT-LAG";

        private ConcurrentDictionary<string, ScheduledCallback> _idCallback;

        protected ConcurrentDictionary<string, ScheduledCallback> IdCallback
        {
            get
            {
                return _idCallback;
            }
        }

        public CallbackScheduler()
        {
            _idCallback = new ConcurrentDictionary<string, ScheduledCallback>();
        }

        #region platform-specific methods
        protected abstract void ScheduleOneTimeCallback(OneTimeCallback callback);
        protected abstract void ScheduleRepeatingCallback(RepeatingCallback callback);
        protected abstract void UnscheduleCallbackPlatformSpecific(string callbackId);
        #endregion

        public void ScheduleCallback(ScheduledCallback callback)
        {
            if (callback.GetType() == typeof(OneTimeCallback))
            {
                OneTimeCallback oneTimeCallback = callback as OneTimeCallback;
                _idCallback[oneTimeCallback.Id] = oneTimeCallback;
                ScheduleOneTimeCallback(oneTimeCallback);
                SensusServiceHelper.Get().Logger.Log("Callback " + oneTimeCallback.Id + " scheduled for " + DateTime.Now.Add(oneTimeCallback.Delay) + " (one-time).", LoggingLevel.Normal, GetType());
            }
            else if (callback.GetType() == typeof(RepeatingCallback))
            {
                RepeatingCallback repeatingCallback = callback as RepeatingCallback;
                _idCallback[repeatingCallback.Id] = callback;
                ScheduleRepeatingCallback(repeatingCallback);
                SensusServiceHelper.Get().Logger.Log("Callback " + repeatingCallback.Id + " scheduled for " + DateTime.Now.Add(repeatingCallback.InitialDelay) + " (repeating).", LoggingLevel.Normal, GetType());
            }
        }

        public bool CallbackIsScheduled(string callbackId)
        {
            // we should never get a null callback id, but it seems that we are from android.
            if (callbackId == null)
            {
                SensusException.Report("Received null callback id.");
                return false;
            }
            else
                return _idCallback.ContainsKey(callbackId);
        }

        public string GetCallbackUserNotificationMessage(string callbackId)
        {
            ScheduledCallback callback;
            _idCallback.TryGetValue(callbackId, out callback);
            return callback?.UserNotificationMessage;
        }

        public DisplayPage GetCallbackDisplayPage(string callbackId)
        {
            ScheduledCallback callback;
            _idCallback.TryGetValue(callbackId, out callback);
            return callback?.DisplayPage ?? DisplayPage.None;
        }

        public virtual Task RaiseCallbackAsync(string callbackId, bool notifyUser, Action<DateTime> scheduleRepeatCallback, Action letDeviceSleepCallback)
        {
            DateTime callbackStartTime = DateTime.Now;

            return Task.Run(async () =>
            {
                try
                {
                    ScheduledCallback scheduledCallback = null;

                    // do we have callback information for the passed callbackId? we might not, in the case where the callback is canceled by the user and the system fires it subsequently.
                    if (!_idCallback.TryGetValue(callbackId, out scheduledCallback))
                    {
                        SensusServiceHelper.Get().Logger.Log("Callback " + callbackId + " is not valid. Unscheduling.", LoggingLevel.Normal, GetType());
                        UnscheduleCallback(callbackId);
                    }

                    if (scheduledCallback != null)
                    {
                        // the same callback action cannot be run multiple times concurrently. drop the current callback if it's already running. multiple
                        // callers might compete for the same callback, but only one will win the lock below and it will exclude all others until it has executed.
                        bool actionAlreadyRunning = true;
                        lock (scheduledCallback)
                        {
                            if (!scheduledCallback.Running)
                            {
                                actionAlreadyRunning = false;
                                scheduledCallback.Running = true;
                            }
                        }

                        if (actionAlreadyRunning)
                            SensusServiceHelper.Get().Logger.Log("Callback \"" + scheduledCallback.Id + "\" is already running. Not running again.", LoggingLevel.Normal, GetType());
                        else
                        {
                            try
                            {
                                if (scheduledCallback.Canceller.IsCancellationRequested)
                                    SensusServiceHelper.Get().Logger.Log("Callback \"" + scheduledCallback.Id + "\" was cancelled before it was raised.", LoggingLevel.Normal, GetType());
                                else
                                {
                                    SensusServiceHelper.Get().Logger.Log("Raising callback \"" + scheduledCallback.Id + "\".", LoggingLevel.Normal, GetType());

                                    if (notifyUser)
                                        SensusContext.Current.Notifier.IssueNotificationAsync("Sensus", scheduledCallback.UserNotificationMessage, callbackId, true, scheduledCallback.DisplayPage);

                                    // if the callback specified a timeout, request cancellation at the specified time.
                                    if (scheduledCallback.CallbackTimeout.HasValue)
                                        scheduledCallback.Canceller.CancelAfter(scheduledCallback.CallbackTimeout.Value);

                                    await scheduledCallback.Action(callbackId, scheduledCallback.Canceller.Token, letDeviceSleepCallback);
                                }
                            }
                            catch (Exception ex)
                            {
                                string errorMessage = "Callback \"" + scheduledCallback.Id + "\" failed:  " + ex.Message;
                                SensusServiceHelper.Get().Logger.Log(errorMessage, LoggingLevel.Normal, GetType());
                                SensusException.Report(errorMessage, ex);
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
                                    if (scheduledCallback.GetType() == typeof(RepeatingCallback))
                                    {
                                        lock (_idCallback)
                                        {
                                            scheduledCallback.Canceller = new CancellationTokenSource();
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                }
                                finally
                                {
                                    // if we marked the callback as running, ensure that we unmark it (note we're nested within two finally blocks so
                                    // this will always execute). this will allow others to run the callback.
                                    lock (scheduledCallback)
                                    {
                                        scheduledCallback.Running = false;
                                    }

                                    // schedule callback again if it was a repeating callback and is still scheduled with a valid repeat delay
                                    if (scheduledCallback.GetType() == typeof(RepeatingCallback) && CallbackIsScheduled(callbackId) && (scheduledCallback as RepeatingCallback).RepeatDelay >= TimeSpan.Zero && scheduleRepeatCallback != null)
                                    {
                                        DateTime nextCallbackTime;

                                        // if this repeating callback is allowed to lag, schedule the repeat from the current time.
                                        if ((scheduledCallback as RepeatingCallback).RepeatLag)
                                            nextCallbackTime = DateTime.Now.Add((scheduledCallback as RepeatingCallback).RepeatDelay);
                                        else
                                        {
                                            // otherwise, schedule the repeat from the time at which the current callback was raised.
                                            nextCallbackTime = callbackStartTime.Add((scheduledCallback as RepeatingCallback).RepeatDelay);
                                        }

                                        scheduleRepeatCallback(nextCallbackTime);
                                    }
                                    else
                                        UnscheduleCallback(callbackId);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = "Failed to raise callback:  " + ex.Message;

                    SensusServiceHelper.Get().Logger.Log(errorMessage, LoggingLevel.Normal, GetType());

                    try
                    {
                        Insights.Report(new Exception(errorMessage, ex), Insights.Severity.Critical);
                    }
                    catch (Exception)
                    {
                    }
                }
            });
        }

        /// <summary>
        /// Cancels a callback that has been raised and is currently executing.
        /// </summary>
        /// <param name="callbackId">Callback identifier.</param>
        public void CancelRaisedCallback(string callbackId)
        {
            ScheduledCallback scheduledCallback;
            if (_idCallback.TryGetValue(callbackId, out scheduledCallback))
            {
                scheduledCallback.Canceller.Cancel();
                SensusServiceHelper.Get().Logger.Log("Cancelled callback \"" + callbackId + "\".", LoggingLevel.Normal, GetType());
            }
            else
                SensusServiceHelper.Get().Logger.Log("Callback \"" + callbackId + "\" not present. Cannot cancel.", LoggingLevel.Normal, GetType());
        }

        /// <summary>
        /// Unschedules the callback, first cancelling any executions that are currently running and then removing the callback from the scheduler.
        /// </summary>
        /// <param name="callbackId">Callback identifier.</param>
        public void UnscheduleCallback(string callbackId)
        {
            if (callbackId != null)
            {
                SensusServiceHelper.Get().Logger.Log("Unscheduling callback \"" + callbackId + "\".", LoggingLevel.Normal, GetType());

                // interrupt any current executions
                CancelRaisedCallback(callbackId);

                // remove from the scheduler
                ScheduledCallback removedCallback;
                _idCallback.TryRemove(callbackId, out removedCallback);

                // tell the current platform cancel its hook into the system's callback architecture
                UnscheduleCallbackPlatformSpecific(callbackId);
            }
        }
    }
}