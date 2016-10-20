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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Sensus.Shared.Context;
using Sensus.Shared.Exceptions;

namespace Sensus.Shared.Callbacks
{
    public abstract class CallbackScheduler : ICallbackScheduler
    {
        private readonly ConcurrentDictionary<string, ScheduledCallback> _idCallback;

        protected CallbackScheduler()
        {
            _idCallback = new ConcurrentDictionary<string, ScheduledCallback>();
        }

        #region platform-specific methods
        protected abstract void ScheduleRepeatingCallback(string callbackId, int initialDelayMS, int repeatDelayMS, bool repeatLag);
        protected abstract void ScheduleOneTimeCallback(string callbackId, int delayMS);
        protected abstract void UnscheduleCallbackPlatformSpecific(string callbackId);
        #endregion

        public string ScheduleRepeatingCallback(ScheduledCallback callback, int initialDelayMS, int repeatDelayMS, bool repeatLag)
        {
            string callbackId = AddCallback(callback);
            ScheduleRepeatingCallback(callbackId, initialDelayMS, repeatDelayMS, repeatLag);
            return callbackId;
        }

        public string RescheduleRepeatingCallback(string callbackId, int initialDelayMS, int repeatDelayMS, bool repeatLag)
        {
            ScheduledCallback scheduledCallback;
            if (_idCallback.TryGetValue(callbackId, out scheduledCallback))
            {
                UnscheduleCallback(callbackId);
                return ScheduleRepeatingCallback(scheduledCallback, initialDelayMS, repeatDelayMS, repeatLag);
            }
            
            return null;
        }

        public string ScheduleOneTimeCallback(ScheduledCallback callback, int delayMS)
        {
            string callbackId = AddCallback(callback);
            ScheduleOneTimeCallback(callbackId, delayMS);
            return callbackId;
        }

        private string AddCallback(ScheduledCallback callback)
        {
            // treat the callback as if it were brand new, even if it might have been previously used (e.g., if it's being reschedueld). set a
            // new ID and cancellation token.
            callback.Id = Guid.NewGuid().ToString();
            callback.Canceller = new CancellationTokenSource();
            _idCallback.TryAdd(callback.Id, callback);
            return callback.Id;
        }

        public bool CallbackIsScheduled(string callbackId)
        {
            return _idCallback.ContainsKey(callbackId);
        }

        public string GetCallbackUserNotificationMessage(string callbackId)
        {
            return _idCallback.ContainsKey(callbackId) ? _idCallback[callbackId].UserNotificationMessage : null;
        }

        public virtual void RaiseCallbackAsync(ICallbackData meta, bool notifyUser, Action<DateTime> scheduleRepeatCallback, Action letDeviceSleepCallback, Action finishedCallback)
        {
            var callbackStartTime = DateTime.Now;
            var callbackId = meta.CallbackId;

            Task.Run(() =>
            {
                try
                {
                    ScheduledCallback scheduledCallback;

                    // do we have callback information for the passed callbackId? we might not, in the case where the callback is canceled by the user and the system fires it subsequently.
                    if (!_idCallback.TryGetValue(callbackId, out scheduledCallback))
                    {
                        SensusServiceHelper.Get().Logger.Log($"Callback {callbackId} is not valid. Unscheduling.", LoggingLevel.Normal, GetType());
                        UnscheduleCallback(callbackId);
                        return;
                    }

                    if (scheduledCallback == null)
                    {
                        return;
                    }

                    if (scheduledCallback.Canceller.IsCancellationRequested)
                    {
                        SensusServiceHelper.Get().Logger.Log($"Callback ({scheduledCallback.Name}) ({callbackId}) was cancelled before it was raised.", LoggingLevel.Normal, GetType());
                        return;
                    }

                    if (AlreadyRunning(scheduledCallback))
                    {
                        SensusServiceHelper.Get().Logger.Log($"Callback ({scheduledCallback.Name}) ({callbackId}) is already running. Not running again.", LoggingLevel.Normal, GetType());
                        return;
                    }

                    try
                    {
                        SensusServiceHelper.Get().Logger.Log($"Raising callback ({scheduledCallback.Name}) ({callbackId}).", LoggingLevel.Normal, GetType());

                        if (notifyUser)
                        {
                            SensusContext.Current.Notifier.IssueNotificationAsync(scheduledCallback.UserNotificationMessage, callbackId, true, true);
                        }

                        if (scheduledCallback.CallbackTimeout.HasValue)
                        {
                            scheduledCallback.Canceller.CancelAfter(scheduledCallback.CallbackTimeout.Value);
                        }

                        scheduledCallback.Action(callbackId, scheduledCallback.Canceller.Token, letDeviceSleepCallback).RunSynchronously();
                    }
                    catch (Exception ex)
                    {
                        SensusException.Report($"Callback ({scheduledCallback.Name}) ({callbackId}) failed:  {ex.Message}", ex);
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
                        lock (scheduledCallback)
                        {
                            scheduledCallback.Canceller = meta.IsRepeating ? new CancellationTokenSource() : scheduledCallback.Canceller;
                            scheduledCallback.Running = false;
                        }


                        // schedule callback again if it was a repeating callback and is still scheduled with a valid repeat delay
                        if (meta.IsRepeating && CallbackIsScheduled(callbackId) && meta.RepeatDelay >= TimeSpan.Zero && scheduleRepeatCallback != null)
                        {
                            scheduleRepeatCallback((meta.LagAllowed ? DateTime.Now : callbackStartTime).Add(meta.RepeatDelay));
                        }
                        else
                        {
                            UnscheduleCallback(callbackId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SensusException.Report($"Failed to raise callback:  {ex.Message}", ex);
                }
                finally
                {
                    finishedCallback?.Invoke();
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

            if (!_idCallback.TryGetValue(callbackId, out scheduledCallback))
            {
                SensusServiceHelper.Get().Logger.Log($"Callback Cancel Failed. ({callbackId}) not present. Cannot cancel.", LoggingLevel.Normal, GetType());
                return;
            }

            lock (scheduledCallback)
            {
                scheduledCallback.Canceller.Cancel();
            }

            SensusServiceHelper.Get().Logger.Log($"Callback Cancel Success {scheduledCallback.Name} ({callbackId}).", LoggingLevel.Normal, GetType());
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

        #region Private Methods
        private static bool AlreadyRunning(ScheduledCallback scheduledCallback)
        {
            // the same callback action cannot be run multiple times concurrently. drop the current callback if it's already running. multiple
            // callers might compete for the same callback, but only one will win the lock below and it will exclude all others until it has executed.
            if (scheduledCallback.Running)
            {
                return true;
            }

            lock (scheduledCallback)
            {
                if (scheduledCallback.Running)
                {
                    return true;
                }

                scheduledCallback.Running = true;
            }

            return false;
        }
        #endregion
    }
}