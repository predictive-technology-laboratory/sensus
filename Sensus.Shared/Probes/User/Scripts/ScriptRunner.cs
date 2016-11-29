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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Specialized;
using Sensus.Concurrent;
using Sensus.Extensions;
using Sensus.UI.UiProperties;
using Sensus.Probes.Location;
using Sensus.Context;
using Sensus.Callbacks;

namespace Sensus.Probes.User.Scripts
{
    public class ScriptRunner
    {
        #region Fields
        private bool _enabled;

        private readonly Dictionary<Trigger, EventHandler<Tuple<Datum, Datum>>> _triggerHandlers;
        private TimeSpan? _maxAge;
        private DateTime? _maxScheduledDate;
        private readonly List<string> _scriptRunCallbackIds;
        private readonly ScheduleTrigger _scheduleTrigger;

        private readonly object _locker = new object();
        #endregion

        #region Properties
        public ScriptProbe Probe { get; set; }

        public Script Script { get; set; }

        [EntryStringUiProperty("Name:", true, 1)]
        public string Name { get; set; }

        [OnOffUiProperty("Enabled:", true, 2)]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (value != _enabled)
                {
                    _enabled = value;

                    if (Probe != null && Probe.Running && _enabled) // probe can be null when deserializing, if set after this property.
                        Start();
                    else if (SensusServiceHelper.Get() != null)  // service helper is null when deserializing
                        Stop();
                }
            }
        }

        [OnOffUiProperty("Allow Cancel:", true, 3)]
        public bool AllowCancel { get; set; }

        public ConcurrentObservableCollection<Trigger> Triggers { get; }

        [EntryDoubleUiProperty("Maximum Age (Mins.):", true, 7)]
        public double? MaxAgeMinutes
        {
            get
            {
                return _maxAge?.TotalMinutes;
            }
            set
            {
                _maxAge = value == null ? default(TimeSpan?) : TimeSpan.FromMinutes(value.Value <= 0 ? 10 : value.Value);
            }
        }

        [OnOffUiProperty("Expire Script When Window Ends:", true, 15)]
        public bool WindowExpiration
        {
            get { return _scheduleTrigger.WindowExpiration; }
            set { _scheduleTrigger.WindowExpiration = value; }
        }

        [EntryStringUiProperty("Random Windows:", true, 8)]
        public string TriggerWindows
        {
            get
            {
                return _scheduleTrigger.Windows;
            }
            set
            {
                _scheduleTrigger.Windows = value;
            }
        }

        public List<DateTime> RunTimes { get; set; }

        public List<DateTime> CompletionTimes { get; set; }

        [OnOffUiProperty("One Shot:", true, 10)]
        public bool OneShot { get; set; }

        [OnOffUiProperty("Run On Start:", true, 11)]
        public bool RunOnStart { get; set; }

        [OnOffUiProperty("Display Progress:", true, 13)]
        public bool DisplayProgress { get; set; }

        [ListUiProperty("Run Mode:", true, 14, new object[] { RunMode.Multiple, RunMode.SingleUpdate, RunMode.SingleKeepOldest })]
        public RunMode RunMode { get; set; }
        #endregion

        #region Constructor
        private ScriptRunner()
        {
            _scheduleTrigger = new ScheduleTrigger(); //this needs to be above
            _enabled = false;
            _maxAge = null;
            _triggerHandlers = new Dictionary<Trigger, EventHandler<Tuple<Datum, Datum>>>();
            _scriptRunCallbackIds = new List<string>();

            Script = new Script(this);
            Triggers = new ConcurrentObservableCollection<Trigger>(new LockConcurrent());
            RunTimes = new List<DateTime>();
            CompletionTimes = new List<DateTime>();
            AllowCancel = true;
            OneShot = false;
            RunOnStart = false;
            DisplayProgress = true;
            RunMode = RunMode.SingleUpdate;

            Triggers.CollectionChanged += (o, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (Trigger trigger in e.NewItems)
                    {
                        // ignore duplicate triggers -- the user should delete and re-add them instead.
                        if (_triggerHandlers.ContainsKey(trigger))
                        {
                            return;
                        }

                        // create a handler to be called each time the triggering probe stores a datum
                        EventHandler<Tuple<Datum, Datum>> handler = (oo, previousCurrentDatum) =>
                        {
                            // must be running and must have a current datum
                            lock (_locker)
                            {
                                if (!Probe.Running || !_enabled || previousCurrentDatum.Item2 == null)
                                {
                                    trigger.FireValueConditionMetOnPreviousCall = false;  // this covers the case when the current datum is null. for some probes, the null datum is meaningful and is emitted in order for their state to be tracked appropriately (e.g., POI probe).
                                    return;
                                }
                            }

                            Datum previousDatum = previousCurrentDatum.Item1;
                            Datum currentDatum = previousCurrentDatum.Item2;

                            // get the value that might trigger the script -- it might be null in the case where the property is nullable and is not set (e.g., facebook fields, input locations, etc.)
                            object currentDatumValue = trigger.DatumProperty.GetValue(currentDatum);
                            if (currentDatumValue == null)
                                return;

                            // if we're triggering based on datum value changes/differences instead of absolute values, calculate the change now.
                            if (trigger.Change)
                            {
                                // don't need to set ConditionSatisfiedLastTime = false here, since it cannot be the case that it's true and prevDatum == null (we must have had a currDatum last time in order to set ConditionSatisfiedLastTime = true).
                                if (previousDatum == null)
                                {
                                    return;
                                }

                                try
                                {
                                    currentDatumValue = Convert.ToDouble(currentDatumValue) - Convert.ToDouble(trigger.DatumProperty.GetValue(previousDatum));
                                }
                                catch (Exception ex)
                                {
                                    SensusServiceHelper.Get().Logger.Log("Trigger error:  Failed to convert datum values to doubles for change calculation:  " + ex.Message, LoggingLevel.Normal, GetType());
                                    return;
                                }
                            }

                            // if the trigger fires for the current value, run a copy of the script so that we can retain a pristine version of the original. use
                            // the async version of run to ensure that we are not on the UI thread.
                            if (trigger.FireFor(currentDatumValue))
                            {
                                RunAsync(new Script(Script, Guid.NewGuid()), previousDatum, currentDatum);
                            }
                        };

                        trigger.Probe.MostRecentDatumChanged += handler;

                        _triggerHandlers.Add(trigger, handler);
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (Trigger trigger in e.OldItems)
                    {
                        if (_triggerHandlers.ContainsKey(trigger))
                        {
                            trigger.Probe.MostRecentDatumChanged -= _triggerHandlers[trigger];

                            _triggerHandlers.Remove(trigger);
                        }
                    }
                }
            };
        }

        public ScriptRunner(string name, ScriptProbe probe) : this()
        {
            Name = name;
            Probe = probe;
        }
        #endregion

        #region Public Methods
        public void Initialize()
        {
            foreach (var trigger in Triggers)
            {
                trigger.Reset();
            }
        }

        public void Start()
        {
            Task.Run(() =>
            {
                UnscheduleCallbacks();
                ScheduleScriptRuns();
            });

            // use the async version below for a couple reasons. first, we're in a non-async method and we want to ensure
            // that the script won't be running on the UI thread. second, from the caller's perspective the prompt should 
            // not need to finish running in order for the runner to be considered started.
            if (RunOnStart)
            {
                RunAsync(new Script(Script, Guid.NewGuid()));
            }
        }

        public bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            return false;
        }

        public void Reset()
        {
            UnscheduleCallbacks();

            RunTimes.Clear();
            CompletionTimes.Clear();
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public void Stop()
        {
            UnscheduleCallbacks();
            SensusServiceHelper.Get().RemoveScriptRunner(this);
        }
        #endregion

        #region Private Methods
        private void ScheduleScriptRuns()
        {
            if (_scheduleTrigger.WindowCount == 0 || SensusServiceHelper.Get() == null || Probe == null || !Probe.Protocol.Running || !_enabled)
            {
                return;
            }

            // get trigger times with respect to the current time occurring after the maximum previously scheduled trigger time.
            foreach (ScriptTriggerTime triggerTime in _scheduleTrigger.GetTriggerTimes(DateTime.Now, _maxScheduledDate.Max(DateTime.Now), _maxAge))
            {
                // don't schedule scripts past the end of the protocol if there's a scheduled end date.
                if (!Probe.Protocol.ContinueIndefinitely && triggerTime.Trigger > Probe.Protocol.EndDate)
                {
                    break;
                }

                // we should always allow at least one future script to be scheduled. this is why the _scheduledCallbackIds collection
                // is a member of the current instances and not global within the script probe. beyond this single scheduled script,
                // only allow a maximum of 32 script-run callbacks to be scheduled. android's limit is 500, and ios 9 has a limit of 64. 
                // not sure about ios 10+. as long as we have just a few script runners, each one will be able to schedule a few future
                // script runs. this will help mitigate the problem of users ignoring surveys and losing touch with the study.
                lock (_scriptRunCallbackIds)
                {
                    if (_scriptRunCallbackIds.Count > 32 / Probe.ScriptRunners.Count)
                    {
                        break;
                    }
                }

                ScheduleScriptRun(triggerTime);
            }
        }

        private void ScheduleScriptRun(ScriptTriggerTime triggerTime)
        {
            // don't bother with the script if it's coming too soon.
            if (triggerTime.ReferenceTillTrigger <= TimeSpan.FromMinutes(1))
            {
                return;
            }

            Script scriptToRun = new Script(Script, Guid.NewGuid()) { ExpirationDate = triggerTime.Expiration, ScheduledRunTime = triggerTime.Trigger };
            ScheduledCallback callback = CreateScriptRunCallback(scriptToRun, triggerTime);
            SensusContext.Current.CallbackScheduler.ScheduleOneTimeCallback(callback, (int)triggerTime.ReferenceTillTrigger.TotalMilliseconds);

            lock (_scriptRunCallbackIds)
            {
                _scriptRunCallbackIds.Add(callback.Id);
            }

            SensusServiceHelper.Get().Logger.Log($"Scheduled for {triggerTime.Trigger} ({callback.Id})", LoggingLevel.Normal, GetType());

            _maxScheduledDate = _maxScheduledDate.Max(triggerTime.Trigger);
        }

        private ScheduledCallback CreateScriptRunCallback(Script script, ScriptTriggerTime triggerTime)
        {
            ScheduledCallback callback = new ScheduledCallback((callbackId, cancellationToken, letDeviceSleepCallback) =>
            {
                return Task.Run(() =>
                {
                    SensusServiceHelper.Get().Logger.Log($"Running script on callback ({callbackId})", LoggingLevel.Normal, GetType());

                    if (!Probe.Running || !_enabled)
                        return;

                    Run(script);

                    lock (_scriptRunCallbackIds)
                    {
                        _scriptRunCallbackIds.Remove(callbackId);
                    }

                    // on android, the callback alarm has fired and the script has been run. on ios, the notification has been
                    // delivered (1) either to the app in the foreground or (2) to the notification tray where the user has opened
                    // it -- either way on ios the app is in the foreground and the script has been run. now is a good time to update 
                    // the scheduled callbacks to run this script.
                    ScheduleScriptRuns();

                }, cancellationToken);

            }, GetType().FullName + "-" + ((long)(triggerTime.Trigger - DateTime.MinValue).TotalDays) + "-" + triggerTime.Window, Script.Id);

#if __IOS__
            // all scheduled scripts with an expiration should show an expiration date to the user. on iOS this will be the only notification for 
            // scheduled surveys, since we don't have a way to update the "you have X pending surveys" notification (generated by triggered 
            // surveys) without executing code in the background.
            if (script.ExpirationDate.HasValue)
            {
                callback.UserNotificationMessage = "Survey expires on " + script.ExpirationDate.Value.ToShortDateString() + " at " + script.ExpirationDate.Value.ToShortTimeString() + ".";
            }
            // on iOS, even if we don't have an expiration date we should show some additional notification, again because we don't have a way
            // to update the "you have X pending surveys" notification from the background.
            else
            {
                callback.UserNotificationMessage = "Please open to take survey.";
            }

            callback.DisplayPage = DisplayPage.PendingSurveys;
#endif

            return callback;
        }

        private void UnscheduleCallbacks()
        {
            lock (_scriptRunCallbackIds)
            {
                if (_scriptRunCallbackIds.Count == 0 || SensusServiceHelper.Get() == null)
                {
                    return;
                }

                foreach (var scheduledCallbackId in _scriptRunCallbackIds)
                {
                    UnscheduleCallback(scheduledCallbackId);
                }

                _scriptRunCallbackIds.Clear();
                _maxScheduledDate = null;
            }
        }

        private void UnscheduleCallback(string scheduledCallbackId)
        {
            SensusContext.Current.CallbackScheduler.UnscheduleCallback(scheduledCallbackId);
            SensusServiceHelper.Get().Logger.Log($"Unscheduled ({scheduledCallbackId})", LoggingLevel.Normal, GetType());
        }

        private Task RunAsync(Script script, Datum previousDatum = null, Datum currentDatum = null)
        {
            return Task.Run(() =>
            {
                Run(script, previousDatum, currentDatum);
            });
        }

        /// <summary>
        /// Run the specified script.
        /// </summary>
        /// <param name="script">Script.</param>
        /// <param name="previousDatum">Previous datum.</param>
        /// <param name="currentDatum">Current datum.</param>
        private void Run(Script script, Datum previousDatum = null, Datum currentDatum = null)
        {
            SensusServiceHelper.Get().Logger.Log($"Running \"{Name}\".", LoggingLevel.Normal, GetType());

            script.RunTime = DateTimeOffset.UtcNow;

            // scheduled scripts have their expiration dates set when they're scheduled. scripts triggered by other probes
            // as well as on-start scripts will not yet have their expiration dates set. so check the script we've been 
            // given and set the expiration date if needed. triggered scripts don't have windows, so the only expiration
            // condition comes from the maximum age.
            if (script.ExpirationDate == null && _maxAge.HasValue)
            {
                script.ExpirationDate = script.Birthdate + _maxAge.Value;
            }

            // script could have already expired (e.g., if user took too long to open notification).
            if (script.ExpirationDate.HasValue && script.ExpirationDate.Value < DateTime.Now)
            {
                SensusServiceHelper.Get().Logger.Log("Script expired before it was run.", LoggingLevel.Normal, GetType());
                return;
            }

            // do not run a one-shot script if it has already been run
            if (OneShot && RunTimes.Count > 0)
            {
                SensusServiceHelper.Get().Logger.Log("Not running one-shot script multiple times.", LoggingLevel.Normal, GetType());
                return;
            }

            lock (RunTimes)
            {
                // track participation by recording the current time. use this instead of the script's run timestamp, since
                // the latter is the time of notification on ios rather than the time that the user actually viewed the script.
                RunTimes.Add(DateTime.Now);
                RunTimes.RemoveAll(r => r < Probe.Protocol.ParticipationHorizon);
            }

            // submit a separate datum indicating each time the script was run.
            Task.Run(async () =>
            {
                // geotag the script-run datum if any of the input groups are also geotagged. if none of the groups are geotagged, then
                // it wouldn't make sense to gather location data from a user.
                double? latitude = null;
                double? longitude = null;
                DateTimeOffset? locationTimestamp = null;
                if (script.InputGroups.Any(inputGroup => inputGroup.Geotag))
                {
                    try
                    {
                        var currentPosition = GpsReceiver.Get().GetReading(new CancellationToken());

                        if (currentPosition == null)
                        {
                            throw new Exception("GPS receiver returned null position.");
                        }

                        latitude = currentPosition.Latitude;
                        longitude = currentPosition.Longitude;
                        locationTimestamp = currentPosition.Timestamp;
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to get position for script-run datum:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                }

                await Probe.StoreDatumAsync(new ScriptRunDatum(script.RunTime.Value, Script.Id, Name, script.Id, script.ScheduledRunTime, script.CurrentDatum?.Id, latitude, longitude, locationTimestamp), default(CancellationToken));
            });

            // this method can be called with previous / current datum values (e.g., when the script is first triggered). it 
            // can also be called without previous / current datum values (e.g., when triggering randomly). if
            // we have such values, set them on the script.

            script.PreviousDatum = previousDatum ?? script.PreviousDatum;
            script.CurrentDatum = currentDatum ?? script.CurrentDatum;

            SensusServiceHelper.Get().AddScriptToRun(script, RunMode);
        }
        #endregion
    }
}