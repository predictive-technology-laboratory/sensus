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
using Sensus.Tools;
using Sensus.Tools.Extensions;
using Sensus.Tools.Scripts;
using SensusUI.UiProperties;
using SensusService.Probes.Location;

namespace SensusService.Probes.User.Scripts
{
    public class ScriptRunner
    {
        #region Fields
        private bool _enabled;        

        private readonly Dictionary<Trigger, EventHandler<Tuple<Datum, Datum>>> _triggerHandlers;

        private DateTime? _maxScheduleDate;
        private readonly List<string> _scheduledCallbackIds;
        private readonly List<ScheduleTrigger> _scheduleTriggers;

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

        [EntryFloatUiProperty("Maximum Age (Mins.):", true, 7)]
        public float? MaxAgeMinutes { get; set; }

        [EntryStringUiProperty("Random Windows:", true, 8)]
        public string TriggerWindows
        {
            get
            {
                lock(_scheduleTriggers)
                {
                    return string.Join(", ", _scheduleTriggers.Select(w => w.ToString()));
                }
            }
            set
            {
                if (value == TriggerWindows) return;

                lock (_scheduleTriggers)
                {
                    _scheduleTriggers.Clear();

                    try
                    {
                        _scheduleTriggers.AddRange(value.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(ScheduleTrigger.Parse));
                    }
                    catch
                    {
                        //ignore improperly formatted trigger windows
                    }
                    
                    _scheduleTriggers.Sort();
                }

                UnscheduleCallbacks();
                ScheduleCallbacks();
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

        [OnOffUiProperty("Expire Script When Window Ends:", true, 15)]
        public bool WindowExpiration { get; set; }
        #endregion

        #region Constructor
        private ScriptRunner()
        {
            Script                = new Script(this);
            _enabled              = false;
            AllowCancel           = true;
            Triggers              = new ConcurrentObservableCollection<Trigger>(new LockConcurrent());
            _triggerHandlers      = new Dictionary<Trigger, EventHandler<Tuple<Datum, Datum>>>();
            MaxAgeMinutes         = null;
            _scheduleTriggers     = new List<ScheduleTrigger>();
            _scheduledCallbackIds = new List<string>();
            RunTimes              = new List<DateTime>();
            CompletionTimes       = new List<DateTime>();
            OneShot               = false;
            RunOnStart            = false;
            DisplayProgress       = true;
            RunMode               = RunMode.SingleUpdate;
            WindowExpiration = false;

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
                                    trigger.FireCriteriaMetOnPreviousCall = false;  // this covers the case when the current datum is null. for some probes, the null datum is meaningful and is emitted in order for their state to be tracked appropriately (e.g., POI probe).
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
                                RunAsync(new Script(Script), previousDatum, currentDatum);
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

        public ScriptRunner(string name, ScriptProbe probe): this()
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
                ScheduleCallbacks();
            });

            // use the async version below for a couple reasons. first, we're in a non-async method and we want to ensure
            // that the script won't be running on the UI thread. second, from the caller's perspective the prompt should 
            // not need to finish running in order for the runner to be considered started.
            if (RunOnStart)
            {
                RunAsync(new Script(Script));
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
            SensusServiceHelper.Get().RemoveScriptsToRun(this);
        }
        #endregion

        #region Private Methods
        private void ScheduleCallbacks()
        {
            if (_scheduleTriggers.Count == 0 || Probe == null || SensusServiceHelper.Get() == null)
            {
                return;
            }

            lock (_scheduleTriggers)
            {
                foreach (var schedule in SchedulesStartingFrom(_maxScheduleDate.Max(DateTime.Now)))
                {
                    if (!Probe.Protocol.ContinueIndefinitely && schedule.RunTime > Probe.Protocol.EndDate)
                    {
                        break;
                    }

                    if (_scheduledCallbackIds.Count > 32 / Probe.ScriptRunners.Count)
                    {
                        break;
                    }

                    if (schedule.RunTime < _maxScheduleDate)
                    {
                        continue;
                    }

                    ScheduleCallback(schedule);
                }
            }            
        }

        private void ScheduleCallback(Schedule schedule)
        {
            if (schedule.TimeUntil <= TimeSpan.FromMinutes(1))
            {
                return;
            }

            lock (_scheduledCallbackIds)
            {
                var timeUntil  = (int)schedule.TimeUntil.TotalMilliseconds;
                var callback   = CreateCallback(new Script(Script) { ExpirationDate = schedule.ExpirationDate });
                var callbackId = SensusServiceHelper.Get().ScheduleOneTimeCallback(callback, timeUntil);

                SensusServiceHelper.Get().Logger.Log($"Scheduled Script Callback for script {Script.Id} at {schedule.RunTime} ({callbackId})", LoggingLevel.Debug , GetType());

                _scheduledCallbackIds.Add(callbackId);
            }

            _maxScheduleDate = _maxScheduleDate.Max(schedule.RunTime);
        }

        private void UnscheduleCallbacks()
        {
            if (_scheduledCallbackIds.Count == 0 || SensusServiceHelper.Get() == null)
            {
                return;
            }            

            lock (_scheduledCallbackIds)
            {
                foreach (var scheduledCallbackId in _scheduledCallbackIds)
                {
                    SensusServiceHelper.Get().UnscheduleCallback(scheduledCallbackId);
                    SensusServiceHelper.Get().Logger.Log($"Unscheduled Script Callback for script {Script.Id} at {DateTime.Now} ({scheduledCallbackId})", LoggingLevel.Debug, GetType());
                }

                _scheduledCallbackIds.Clear();
                _maxScheduleDate = null;
            }
        }        

        private ScheduledCallback CreateCallback(Script script)
        {
            var callback = new ScheduledCallback("Trigger Randomly", (callbackId, cancellationToken, letDeviceSleepCallback) =>
            {
                return Task.Run(() =>
                {
                    SensusServiceHelper.Get().Logger.Log($"Executed Script Callback for script {Script.Id} at {DateTime.Now} ({callbackId})", LoggingLevel.Debug, GetType());

                    if (!Probe.Running || !_enabled) return;                    

                    Run(script);

                    ScheduleCallbacks();

                }, cancellationToken);
            });

#if __IOS__
            // we won't have a way to update the "X Pending Surveys" notification on ios. the best we can do is display a new notification describing the survey and showing its expiration time (if there is one).                                                
            if (script.ExpirationDate < DateTime.MaxValue)
            {
                callback.UserNotificationMessage = "Please open to take survey. Expires " + script.ExpirationDate.ToString("on MM/dd/yy at HH\\:mm\\:ss") +  ".";
            }
            else
            {
                callback.UserNotificationMessage = "Please open to take survey.";
            }
            
            // on ios we need a separate indicator that the surveys page should be displayed when the user opens the notification. this is achieved by setting the notification ID to the pending survey notification ID.
            callback.NotificationId = SensusServiceHelper.PENDING_SURVEY_NOTIFICATION_ID;
#endif

            return callback;
        }

        private IEnumerable<Schedule> SchedulesStartingFrom(DateTime startDate)
        {
            var ageExpiration  = MaxAgeMinutes == null ? (TimeSpan?)null : TimeSpan.FromMinutes(MaxAgeMinutes.Value);
            var futureDistance = TimeSpan.FromDays(8);

            lock (_scheduleTriggers)
            {
                for (var currentDate = startDate; currentDate - startDate < futureDistance && currentDate - DateTime.Now < futureDistance; currentDate += TimeSpan.FromDays(1))
                {
                    foreach (var scheduleTrigger in _scheduleTriggers)
                    {
                        yield return scheduleTrigger.NextSchedule(DateTime.Now, currentDate, WindowExpiration, ageExpiration);
                    }
                }
            }
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

            // on android, scripts are always run as scheduled (even in the background), so we just set the run timestamp
            // here. on ios, trigger-based scripts are run on demand (even in the background), so we can also set the 
            // timestamp here. schedule-based scripts have their run timestamp set to the UILocalNotification fire time, and
            // this is done prior to calling the current method. so we shouldn't reset the run timestamp here.
            script.RunTimestamp = script.RunTimestamp ?? DateTimeOffset.UtcNow;

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

                await Probe.StoreDatumAsync(new ScriptRunDatum(script.RunTimestamp.Value, Script.Id, Name, script.Id, script.CurrentDatum?.Id, latitude, longitude, locationTimestamp), default(CancellationToken));
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