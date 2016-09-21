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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using SensusUI.UiProperties;
using System.Collections.Specialized;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using Plugin.Geolocator.Abstractions;
using SensusService.Probes.Location;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace SensusService.Probes.User.Scripts
{
    public class ScriptRunner
    {
        private string _name;
        private ScriptProbe _probe;
        private Script _script;
        private bool _enabled;
        private bool _allowCancel;
        private ObservableCollection<Trigger> _triggers;
        private Dictionary<Trigger, EventHandler<Tuple<Datum, Datum>>> _triggerHandler;
        private float? _maximumAgeMinutes;
        private List<Tuple<DateTime, DateTime, DateTime?>> _triggerWindows;     // first two items are start/end of window. last item is date of last run
        private ConcurrentDictionary<string, Tuple<DateTime, DateTime>> _triggerWindowCallbacks;
        private Random _random;
        private List<DateTime> _runTimes;
        private List<DateTime> _completionTimes;
        private bool _oneShot;
        private bool _runOnStart;
        private bool _displayProgress;
        private RunMode _runMode;
        private bool _invalidateScriptWhenWindowEnds;

        private readonly object _locker = new object();

        #region properties

        public ScriptProbe Probe
        {
            get
            {
                return _probe;
            }
            set
            {
                _probe = value;
            }
        }

        public Script Script
        {
            get
            {
                return _script;
            }
            set
            {
                _script = value;
            }
        }

        [EntryStringUiProperty("Name:", true, 1)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

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

                    if (_probe != null && _probe.Running && _enabled) // probe can be null when deserializing, if set after this property.
                        Start();
                    else if (SensusServiceHelper.Get() != null)  // service helper is null when deserializing
                        Stop();
                }
            }
        }

        [OnOffUiProperty("Allow Cancel:", true, 3)]
        public bool AllowCancel
        {
            get
            {
                return _allowCancel;
            }
            set
            {
                _allowCancel = value;
            }
        }

        public ObservableCollection<Trigger> Triggers
        {
            get { return _triggers; }
        }

        [EntryFloatUiProperty("Maximum Age (Mins.):", true, 7)]
        public float? MaximumAgeMinutes
        {
            get { return _maximumAgeMinutes; }
            set { _maximumAgeMinutes = value; }
        }

        [EntryStringUiProperty("Random Windows:", true, 8)]
        public string TriggerWindows
        {
            get
            {
                if (_triggerWindows.Count == 0)
                    return "";
                else
                    return string.Concat(_triggerWindows.Select((window, index) => (index == 0 ? "" : ", ") +
                            (
                                window.Item1 == window.Item2 ? window.Item1.Hour + ":" + window.Item1.Minute.ToString().PadLeft(2, '0') :
                                window.Item1.Hour + ":" + window.Item1.Minute.ToString().PadLeft(2, '0') + "-" + window.Item2.Hour + ":" + window.Item2.Minute.ToString().PadLeft(2, '0')
                            )));
            }
            set
            {
                if (value == TriggerWindows)
                    return;

                _triggerWindows.Clear();

                try
                {
                    foreach (string window in value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] startEnd = window.Trim().Split('-');

                        DateTime start = DateTime.Parse(startEnd[0].Trim());
                        DateTime end = start;

                        if (startEnd.Length > 1)
                        {
                            end = DateTime.Parse(startEnd[1].Trim());

                            if (start > end)
                                throw new Exception();
                        }

                        _triggerWindows.Add(new Tuple<DateTime, DateTime, DateTime?>(start, end, null));
                    }
                }
                catch (Exception)
                {
                }

                // sort windows by increasing hour and minute of the window start (day, month, and year are irrelevant)
                _triggerWindows.Sort((window1, window2) =>
                {
                    int hourComparison = window1.Item1.Hour.CompareTo(window2.Item1.Hour);

                    if (hourComparison != 0)
                        return hourComparison;
                    else
                        return window1.Item1.Minute.CompareTo(window2.Item1.Minute);
                });

                if (_probe != null && _probe.Running && _enabled && _triggerWindows.Count > 0)  // probe can be null during deserialization if this property is set first
                    StartTriggerCallbacksAsync();
                else if (SensusServiceHelper.Get() != null)  // service helper can be null when deserializing
                    StopRandomTriggerCallbackAsync();
            }
        }

        public ConcurrentDictionary<string, Tuple<DateTime, DateTime>> TriggerWindowCallbacks
        {
            get { return _triggerWindowCallbacks; }
        }

        public List<DateTime> RunTimes
        {
            get
            {
                return _runTimes;
            }
            set
            {
                _runTimes = value;
            }
        }

        public List<DateTime> CompletionTimes
        {
            get
            {
                return _completionTimes;
            }
            set
            {
                _completionTimes = value;
            }
        }

        [OnOffUiProperty("One Shot:", true, 10)]
        public bool OneShot
        {
            get
            {
                return _oneShot;
            }
            set
            {
                _oneShot = value;
            }
        }

        [OnOffUiProperty("Run On Start:", true, 11)]
        public bool RunOnStart
        {
            get
            {
                return _runOnStart;
            }
            set
            {
                _runOnStart = value;
            }
        }

        [OnOffUiProperty("Display Progress:", true, 13)]
        public bool DisplayProgress
        {
            get
            {
                return _displayProgress;
            }
            set
            {
                _displayProgress = value;
            }
        }

        [ListUiProperty("Run Mode:", true, 14, new object[] { RunMode.Multiple, RunMode.SingleUpdate, RunMode.SingleKeepOldest })]
        public RunMode RunMode
        {
            get
            {
                return _runMode;
            }

            set
            {
                _runMode = value;
            }
        }

        #endregion

        /// <summary>
        /// For JSON.NET deserialization.
        /// </summary>
        private ScriptRunner()
        {
            _script = new Script(this);
            _enabled = false;
            _allowCancel = true;
            _triggers = new ObservableCollection<Trigger>();
            _triggerHandler = new Dictionary<Trigger, EventHandler<Tuple<Datum, Datum>>>();
            _maximumAgeMinutes = null;
            _triggerWindows = new List<Tuple<DateTime, DateTime, DateTime?>>();
            _triggerWindowCallbacks = new ConcurrentDictionary<string, Tuple<DateTime, DateTime>>();
            _random = new Random();
            _runTimes = new List<DateTime>();
            _completionTimes = new List<DateTime>();
            _oneShot = false;
            _runOnStart = false;
            _displayProgress = true;
            _runMode = RunMode.SingleUpdate;
            _invalidateScriptWhenWindowEnds = false;

            _triggers.CollectionChanged += (o, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (Trigger trigger in e.NewItems)
                    {
                        // ignore duplicate triggers -- the user should delete and re-add them instead.
                        if (_triggerHandler.ContainsKey(trigger))
                            return;

                        // create a handler to be called each time the triggering probe stores a datum
                        EventHandler<Tuple<Datum, Datum>> handler = (oo, previousCurrentDatum) =>
                        {
                            // must be running and must have a current datum
                            lock (_locker)
                            {
                                if (!_probe.Running || !_enabled || previousCurrentDatum.Item2 == null)
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
                                    return;

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
                                RunAsync(_script.Copy(), previousDatum, currentDatum);
                        };

                        trigger.Probe.MostRecentDatumChanged += handler;

                        _triggerHandler.Add(trigger, handler);
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                    foreach (Trigger trigger in e.OldItems)
                        if (_triggerHandler.ContainsKey(trigger))
                        {
                            trigger.Probe.MostRecentDatumChanged -= _triggerHandler[trigger];

                            _triggerHandler.Remove(trigger);
                        }
            };
        }

        public ScriptRunner(string name, ScriptProbe probe)
            : this()
        {
            _name = name;
            _probe = probe;
        }

        public void Initialize()
        {
            foreach (Trigger trigger in _triggers)
                trigger.Reset();
        }

        public void Start()
        {
            if (_triggerWindows.Count > 0)
                StartTriggerCallbacksAsync();

            // use the async version below for a couple reasons. first, we're in a non-async method and we want to ensure
            // that the script won't be running on the UI thread. second, from the caller's perspective the prompt should 
            // not need to finish running in order for the runner to be considered started.
            if (_runOnStart)
                RunAsync(_script.Copy());
        }

        private void StartTriggerCallbacksAsync()
        {
            new Thread(() =>
            {
                StartTriggerCallbacks();

            }).Start();
        }

        private void ScheduleTriggerWindowCallback(Tuple<DateTime, DateTime, DateTime?> triggerWindow, bool useCurrentTimeAsStart)
        {
            DateTime triggerWindowStart = useCurrentTimeAsStart ? DateTime.Now : triggerWindow.Item1;
            DateTime triggerWindowEnd = triggerWindow.Item2;
            DateTime? triggerWindowLastRun = triggerWindow.Item3;

            ScheduledCallback callback = new ScheduledCallback((callbackId, cancellationToken, letDeviceSleepCallback) => RunUponCallback(callbackId, triggerWindow), "Trigger Randomly");

#if __IOS__
            // we won't have a way to update the "X Pending Surveys" notification on ios. the best we can do is
            // display a new notification describing the survey and showing its expiration time (if there is one).
            string userNotificationMessage = "Please open to take survey.";
            if (_maximumAgeMinutes.HasValue)
            {
                DateTime expirationTime = triggerTime.AddMinutes(_maximumAgeMinutes.Value);
                userNotificationMessage += " Expires on " + expirationTime.ToShortDateString() + " at " + expirationTime.ToShortTimeString() + ".";
            }

            callback.UserNotificationMessage = userNotificationMessage;

            // on ios we need a separate indicator that the surveys page should be displayed when the user opens
            // the notification. this is achieved by setting the notification ID to the pending survey notification ID.
            callback.NotificationId = SensusServiceHelper.PENDING_SURVEY_NOTIFICATION_ID;
#endif

            if (!_triggerWindowCallbacks.TryAdd(SensusServiceHelper.Get().ScheduleOneTimeCallback(callback, ((int)((triggerWindowStart.AddSeconds(_random.NextDouble() * (triggerWindowEnd - triggerWindowStart).TotalSeconds)) - DateTime.Now).TotalMilliseconds)), new Tuple<DateTime, DateTime>(triggerWindowStart, triggerWindowEnd)))
                SensusServiceHelper.Get().Logger.Log("Unable to schedule random trigger window callback: " + triggerWindowStart + " - " + triggerWindowEnd + ".", LoggingLevel.Normal, GetType());
        }

        private Task RunUponCallback(string callbackId, Tuple<DateTime, DateTime, DateTime?> triggerWindow)
        {
            return Task.Run(() =>
                {
                    // if the probe is still running and the runner is enabled, run a copy of the script so that we can retain a pristine version of the original.
                    // also, when the script prompts display let the caller know that it's okay for the device to sleep.
                    if (_probe.Running && _enabled)
                    {
                        Script scriptCopyToRun = _script.Copy();

                        // update this trigger window's last run date
                        triggerWindow = new Tuple<DateTime, DateTime, DateTime?>(triggerWindow.Item1, triggerWindow.Item2, DateTime.Now.Date);

                        // expose the script's callback id so we can access the window it's running in from SensusServiceHelper
                        scriptCopyToRun.CallbackId = callbackId;

                        Run(scriptCopyToRun);
                    }
                });
        }

        private void StartTriggerCallbacks()
        {
            lock (_locker)
            {
                if (_triggerWindows.Count == 0)
                    return;

                StopTriggerCallbacks();

                SensusServiceHelper.Get().Logger.Log("Starting random script trigger callbacks.", LoggingLevel.Normal, GetType());

                DateTime triggerWindowStart = default(DateTime);
                DateTime triggerWindowEnd = default(DateTime);
                DateTime? triggerWindowLastRun = null;
                DateTime now = DateTime.Now;

                // find trigger windows, ignoring month, day, and year. the windows are already sorted.
                // schedule callbacks for two weeks.
                foreach (Tuple<DateTime, DateTime, DateTime?> triggerWindow in _triggerWindows)
                {
                    triggerWindowStart = new DateTime(now.Year, now.Month, now.Day, triggerWindow.Item1.Hour, triggerWindow.Item1.Minute, 0);
                    triggerWindowEnd = new DateTime(now.Year, now.Month, now.Day, triggerWindow.Item2.Hour, triggerWindow.Item2.Minute, 0);
                    if (triggerWindow.Item3.HasValue)
                        triggerWindowLastRun = triggerWindow.Item3.Value;

                    // if this window has not yet started today, schedule a callback within it
                    if (triggerWindowStart.Hour > now.Hour || (triggerWindowStart.Hour == now.Hour && triggerWindowStart.Minute > now.Minute && triggerWindowEnd.Minute > now.Minute))
                    {
                        ScheduleTriggerWindowCallback(triggerWindow, false);
                    }

                    // if we're inside the window with more than 5 minutes left and the window
                    // hasn't run yet today, schedule a callback within the remaining time
                    else if ((triggerWindowLastRun.HasValue && triggerWindowLastRun.Value.Date < DateTime.Now.Date) && (triggerWindowEnd.Hour > now.Hour || (triggerWindowEnd.Hour == now.Hour && triggerWindowEnd.Minute > now.AddMinutes(5).Minute)))
                    {
                        ScheduleTriggerWindowCallback(triggerWindow, true);
                    }

                    // schedule callbacks for the next two weeks
                    Tuple<DateTime, DateTime, DateTime?> nextTriggerWindow = new Tuple<DateTime, DateTime, DateTime?>(triggerWindowStart.AddDays(1), triggerWindowEnd.AddDays(1), triggerWindowLastRun);
                    DateTime protocolStopTime = new DateTime(_probe.Protocol.EndDate.Year, _probe.Protocol.EndDate.Month, _probe.Protocol.EndDate.Day, _probe.Protocol.EndTime.Hours, _probe.Protocol.EndTime.Minutes, 0);
                    int horizon = 0;
                    while (nextTriggerWindow.Item2 < protocolStopTime && horizon < 14)
                    {
                        ScheduleTriggerWindowCallback(nextTriggerWindow, false);

                        nextTriggerWindow = new Tuple<DateTime, DateTime, DateTime?>(nextTriggerWindow.Item1.AddDays(1), nextTriggerWindow.Item2.AddDays(1), nextTriggerWindow.Item3);

                        horizon += 1;
                    }
                }
            }
        }

        private void RunAsync(Script script, Datum previousDatum = null, Datum currentDatum = null, Action callback = null)
        {
            new Thread(() =>
            {
                Run(script, previousDatum, currentDatum);

                if (callback != null)
                    callback();

            }).Start();
        }

        /// <summary>
        /// Run the specified script.
        /// </summary>
        /// <param name="script">Script.</param>
        /// <param name="previousDatum">Previous datum.</param>
        /// <param name="currentDatum">Current datum.</param>
        private void Run(Script script, Datum previousDatum = null, Datum currentDatum = null)
        {
            SensusServiceHelper.Get().Logger.Log("Running \"" + _name + "\".", LoggingLevel.Normal, GetType());

            // on android, scripts are always run as scheduled (even in the background), so we just set the run timestamp
            // here. on ios, trigger-based scripts are run on demand (even in the background), so we can also set the 
            // timestamp here. schedule-based scripts have their run timestamp set to the UILocalNotification fire time, and
            // this is done prior to calling the current method. so we shouldn't reset the run timestamp here.
            if (script.RunTimestamp == null)
                script.RunTimestamp = DateTimeOffset.UtcNow;

            lock (_runTimes)
            {
                // do not run a one-shot script if it has already been run
                if (_oneShot && _runTimes.Count > 0)
                {
                    SensusServiceHelper.Get().Logger.Log("Not running one-shot script multiple times.", LoggingLevel.Normal, GetType());
                    return;
                }

                // track participation by recording the current time. use this instead of the script's run timestamp, since
                // the latter is the time of notification on ios rather than the time that the user actually viewed the script.
                _runTimes.Add(DateTime.Now);
                _runTimes.RemoveAll(r => r < _probe.Protocol.ParticipationHorizon);
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
                        Position currentPosition = GpsReceiver.Get().GetReading(default(CancellationToken));

                        if (currentPosition == null)
                            throw new Exception("GPS receiver returned null position.");

                        latitude = currentPosition.Latitude;
                        longitude = currentPosition.Longitude;
                        locationTimestamp = currentPosition.Timestamp;
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to get position for script-run datum:  " + ex.Message, LoggingLevel.Normal, GetType());
                    }
                }

                await _probe.StoreDatumAsync(new ScriptRunDatum(script.RunTimestamp.Value, _script.Id, _name, script.Id, script.CurrentDatum?.Id, latitude, longitude, locationTimestamp), default(CancellationToken));
            });

            // this method can be called with previous / current datum values (e.g., when the script is first triggered). it 
            // can also be called without previous / current datum values (e.g., when triggering randomly). if
            // we have such values, set them on the script.

            if (previousDatum != null)
                script.PreviousDatum = previousDatum;

            if (currentDatum != null)
                script.CurrentDatum = currentDatum;

            SensusServiceHelper.Get().AddScriptToRun(script, _runMode);
        }

        public void RescheduleTriggerCallbacks()
        {
            StartTriggerCallbacksAsync();
        }

        public bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            return false;
        }

        public void Reset()
        {
            _triggerWindowCallbacks.Clear();
            _runTimes.Clear();
            _completionTimes.Clear();
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        private void StopRandomTriggerCallbackAsync()
        {
            new Thread(() =>
                {
                    StopTriggerCallbacks();

                }).Start();
        }

        private void StopTriggerCallbacks()
        {
            String logMessage = "Stopping random trigger callbacks.";
            if (!_triggerWindowCallbacks.IsEmpty)
            {
                foreach (var triggerCallbackId in _triggerWindowCallbacks.Keys)
                {
                    SensusServiceHelper.Get().UnscheduleCallback(triggerCallbackId);
                }
                _triggerWindowCallbacks.Clear();
            }
            else
            {
                logMessage += ".. none to stop.";
            }
            SensusServiceHelper.Get().Logger.Log(logMessage, LoggingLevel.Normal, GetType());
        }

        public void Stop()
        {
            StopTriggerCallbacks();
            SensusServiceHelper.Get().RemoveScriptsToRun(this);
        }
    }
}