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
        #region private classes

        private struct TriggerWindow: IComparable<TriggerWindow>
        {
            #region Static Methods
            public static TriggerWindow Parse(string value)
            {
                return new TriggerWindow(value);
            }
            #endregion
            
            #region Properties
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public DateTime? MostRecentRun { get; set; }
            #endregion

            #region Constructors

            public TriggerWindow(TriggerWindow window)
            {
                Start         = window.Start;
                End           = window.End;
                MostRecentRun = window.MostRecentRun;
            }

            private TriggerWindow(string window)
            {
                var startEnd = window.Trim().Split('-');

                Start         = DateTime.Parse(startEnd[0].Trim());
                End           = startEnd.Length == 1 ? Start : DateTime.Parse(startEnd[1].Trim());
                MostRecentRun = null;

                if (Start > End)
                {
                    throw new Exception($"Improper trigger window ({window})");
                }
            }
            #endregion

            #region Public Methods
            public int MillisecondsToTrigger()
            {
                // if the window's start and end are the same, it's a specific time.
                // just use the length of time between now and the window.
                if (End == Start) return (int)(End - DateTime.Now).TotalMilliseconds;

                // otherwise it's a random window. find the length of the window,
                // generate a random offset into the window, add that duration to
                // the window's start time, and find the length of time between now and that time.
                var windowLength  = (End - Start).TotalMilliseconds;
                var zeroToOne = new Random((int)DateTime.Now.Ticks).NextDouble();
                var offsetIntoWindow = windowLength * zeroToOne;
                var startPlusOffset = Start.AddMilliseconds(offsetIntoWindow);

                return (int)(startPlusOffset - DateTime.Now).TotalMilliseconds;
            }

            public bool StartsLater()
            {
                return Start.TimeOfDay > DateTime.Now.TimeOfDay;
            }

            public bool EndsLater()
            {
                return End.TimeOfDay > DateTime.Now.TimeOfDay.Add(new TimeSpan(0, 5, 0));
            }

            public bool HasNotRunToday()
            {
                return !(MostRecentRun.HasValue) || MostRecentRun.Value.Date < DateTime.Now.Date;
            }

            public override string ToString()
            {
                return Start == End ? HourMinute(Start.TimeOfDay) : $"{HourMinute(Start.TimeOfDay)}-{HourMinute(End.TimeOfDay)}";
            }

            public int CompareTo(TriggerWindow compare)
            {
                int hourComparison = Start.Hour.CompareTo(compare.Start.Hour);

                if (hourComparison != 0)
                {
                    return hourComparison;
                }
                else
                {
                    return Start.Minute.CompareTo(compare.Start.Minute);
                }

            }
            #endregion

            #region Private Methods
            private string HourMinute(TimeSpan time)
            {
                return $"{time.Hours}:{time.Minutes.ToString().PadLeft(2, '0')}";
            }
            #endregion
        }

        #endregion

        private string _name;
        private ScriptProbe _probe;
        private Script _script;
        private bool _enabled;
        private bool _allowCancel;
        private ObservableCollection<Trigger> _triggers;
        private Dictionary<Trigger, EventHandler<Tuple<Datum, Datum>>> _triggerHandler;
        private float? _maximumAgeMinutes;
        private List<TriggerWindow> _triggerWindows;     // first two items are start/end of window. last item is date of last run
        private Dictionary<string, Tuple<DateTime, DateTime>> _triggerWindowCallbacks;
        private String _mostRecentTriggerWindowCallback;
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
                lock(_triggerWindows)
                {
                    return string.Join(", ", _triggerWindows.Select(w => w.ToString()));
                }
            }
            set
            {
                if (value == TriggerWindows) return;

                lock(_triggerWindows)
                {
                    _triggerWindows.Clear();

                    try
                    {
                        _triggerWindows.AddRange(value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(TriggerWindow.Parse));
                    }
                    catch
                    {
                        //ignore improperly formatted trigger windows
                    }

                    // sort windows by increasing hour and minute of the window start (day, month, and year are irrelevant)
                    _triggerWindows.Sort();
                }
                // probe can be null during deserialization if this property is set first
                if (_probe != null && _probe.Running && _enabled && _triggerWindows.Count > 0)
                {
                    StartTriggerCallbacksAsync();
                }

                // service helper can be null when deserializing
                else if (SensusServiceHelper.Get() != null)  
                {
                    StopRandomTriggerCallbackAsync();
                }
            }
        }

        public Dictionary<string, Tuple<DateTime, DateTime>> TriggerWindowCallbacks
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

        [OnOffUiProperty("Invalidate Script When Window Ends:", true, 15)]
        public bool InvalidateScriptWhenWindowEnds
        {
            get { return _invalidateScriptWhenWindowEnds; }
            set { _invalidateScriptWhenWindowEnds = value; }
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
            _triggerWindows = new List<TriggerWindow>();
            _triggerWindowCallbacks = new Dictionary<string, Tuple<DateTime, DateTime>>();
            _mostRecentTriggerWindowCallback = null;
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
            if (_triggerWindows.Any())
                StartTriggerCallbacksAsync();

            // use the async version below for a couple reasons. first, we're in a non-async method and we want to ensure
            // that the script won't be running on the UI thread. second, from the caller's perspective the prompt should 
            // not need to finish running in order for the runner to be considered started.
            if (_runOnStart)
                RunAsync(_script.Copy());
        }

        private Task StartTriggerCallbacksAsync()
        {
            return Task.Run(() =>
            {
                StartTriggerCallbacks();
            });
        }

        private void ScheduleTriggerWindowCallback(TriggerWindow triggerWindow)
        {
            int millisecondsToTrigger = triggerWindow.MillisecondsToTrigger();

            if (millisecondsToTrigger < 0)
            {
                return;
            }

            var callback = new ScheduledCallback((callbackId, cancellationToken, letDeviceSleepCallback) =>
            {
                return Task.Run(() =>
                {
                    // if the probe is still running and the runner is enabled, run a copy of the script so that we can retain a pristine version of the original.
                    // also, when the script prompts display let the caller know that it's okay for the device to sleep.
                    if (!_probe.Running || !_enabled) return;

                    Script scriptCopyToRun = _script.Copy();

                    // Replace the element of _triggerWindows with start/end that match the current triggerWindow, setting 
                    // the last-fired time to DateTime.Now.Date. make sure to lock _triggerWindows prior to iterating. also, prevent
                    // user from entering the same window twice (in setter).
                    lock(_triggerWindows)
                    {
                        int replaceIndex = _triggerWindows.FindIndex(window => window.Start.TimeOfDay == triggerWindow.Start.TimeOfDay && window.End.TimeOfDay == triggerWindow.End.TimeOfDay);
                        _triggerWindows[replaceIndex] = new TriggerWindow(triggerWindow) { MostRecentRun = DateTime.Now };
                    }

                    // attach run time to script so we can measure age
                    //scriptCopyToRun.RunTimestamp = DateTime.Now.AddMilliseconds(millisecondsToTrigger);
                    scriptCopyToRun.RunTimestamp = DateTime.Now;

                    // expose the script's callback id so we can access the window it's running in from SensusServiceHelper
                    scriptCopyToRun.CallbackId = callbackId;

                    // run the script
                    Run(scriptCopyToRun);

                    // check trigger window callbacks and reschedule if needed
                    ScheduleTriggerCallbacks();
                }, cancellationToken);
            }, "Trigger Randomly");



#if __IOS__
            // we won't have a way to update the "X Pending Surveys" notification on ios. the best we can do is display a new notification describing the survey and showing its expiration time (if there is one).                                                
            if (_maximumAgeMinutes.HasValue)
            {
                DateTime expirationTimestamp = DateTime.Now.AddMilliseconds(millisecondsToTrigger).AddMinutes(_maximumAgeMinutes.Value);
                callback.UserNotificationMessage = $"Please open to take survey. Expires on " + expirationTimestamp.ToShortDateString() + " at " + expirationTimestamp.ToShortTimeString() + ".";
            }
            else
            {
                callback.UserNotificationMessage = "Please open to take survey.";
            }
            
            // on ios we need a separate indicator that the surveys page should be displayed when the user opens the notification. this is achieved by setting the notification ID to the pending survey notification ID.
            callback.NotificationId = SensusServiceHelper.PENDING_SURVEY_NOTIFICATION_ID;
#endif

            String triggerWindowCallbackId = SensusServiceHelper.Get().ScheduleOneTimeCallback(callback, millisecondsToTrigger);

            lock (_triggerWindowCallbacks) 
            {
                _triggerWindowCallbacks.Add(triggerWindowCallbackId, new Tuple<DateTime, DateTime>(triggerWindow.Start, triggerWindow.End));
            }

            _probe.ScriptCallbacksScheduled += 1;
            _mostRecentTriggerWindowCallback = triggerWindowCallbackId;
        }

        private int CallbackAllocation()
        {
            int totalTriggerWindows = 0;

            foreach (ScriptRunner runner in _probe.ScriptRunners)
            {
                if (!runner.Enabled)
                    continue;
                
                totalTriggerWindows += runner.TriggerWindows.Count();
            }

            double proportion = (double) this.TriggerWindows.Count() / (double) totalTriggerWindows;

            return (int) Math.Floor(proportion * 32);
        }

        /// <remarks>
        /// Build a list of trigger windows to schedule, stopping when any of the following is reached:
        /// 1) the day the protocol is scheduled to stop
        /// 2) 28 days into the future (measuring delays in milliseconds leads to integer overflow)
        /// 3) maximum allowed callbacks (in proportion to the number required per day, as compared to other enabled scripts
        /// </remarks>
        private IEnumerable<TriggerWindow> FindTriggerCallbacks()
        {
            var triggerWindowsToSchedule = new List<TriggerWindow>();

            var protocolStop       = new DateTime(_probe.Protocol.EndDate.Year, _probe.Protocol.EndDate.Month, _probe.Protocol.EndDate.Day, _probe.Protocol.EndTime.Hours, _probe.Protocol.EndTime.Minutes, 0);
            var dayIndexMax        = _probe.Protocol.ScheduledStopCallbackId != null ? protocolStop.Subtract(DateTime.Now).Days + 2 : 28;
            var callbackAllocation = CallbackAllocation();

            for (var dayIndex = 0; dayIndex < dayIndexMax && dayIndex < 28 && triggerWindowsToSchedule.Count < callbackAllocation; dayIndex++)
            {
                triggerWindowsToSchedule.AddRange(FindTriggerCallbacksByDay(dayIndex, dayIndexMax, protocolStop));
            }

            return triggerWindowsToSchedule;
        }

        private IEnumerable<TriggerWindow> FindTriggerCallbacksByDay(int dayIndex, int dayIndexMax, DateTime protocolStop)
        {
            lock(_triggerWindows)
            {
                foreach (var triggerWindow in _triggerWindows)
                {
                    var window = new TriggerWindow
                    {
                        Start         = triggerWindow.Start.AddDays(dayIndex),
                        End           = triggerWindow.End.AddDays(dayIndex),
                        MostRecentRun = triggerWindow.MostRecentRun
                    };

                    // skip already scheduled scripts
                    if (_mostRecentTriggerWindowCallback != null && window.End <= _triggerWindowCallbacks[_mostRecentTriggerWindowCallback].Item2)
                    {
                        continue;
                    }

                    if (dayIndex == 0 && window.StartsLater())
                    {
                        yield return window;
                    }

                    else if (dayIndex == 0 && window.EndsLater() && window.HasNotRunToday())
                    {
                        yield return new TriggerWindow(window) { Start = DateTime.Now };
                    }

                    else if (0 < dayIndex && dayIndex + 1 < dayIndexMax)
                    {
                        yield return window;
                    }

                    else if (window.Start.AddMinutes(5) <= protocolStop)
                    {
                        yield return window;
                    }
                }
            }
        }

        public void ScheduleTriggerCallbacks()
        {
            lock (_locker)
            {
                if (!_triggerWindows.Any())
                    return;

                SensusServiceHelper.Get().Logger.Log("Scheduling script trigger callbacks.", LoggingLevel.Normal, GetType());

                // schedule the callbacks
                foreach (var triggerWindowToSchedule in FindTriggerCallbacks())
                {
                    ScheduleTriggerWindowCallback(triggerWindowToSchedule);
                }
            }
        }

        private void StartTriggerCallbacks()
        {
            StopTriggerCallbacks();
            SensusServiceHelper.Get().Logger.Log("Starting script trigger callbacks.", LoggingLevel.Normal, GetType());
            ScheduleTriggerCallbacks();
        }

        private Task RunAsync(Script script, Datum previousDatum = null, Datum currentDatum = null, Action callback = null)
        {
            return Task.Run(() =>
            {
                Run(script, previousDatum, currentDatum);

                if (callback != null)
                    callback();

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
        
        public bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            return false;
        }

        public void Reset()
        {
            lock (_triggerWindowCallbacks)
            {
                _triggerWindowCallbacks.Clear();
            }
            _runTimes.Clear();
            _completionTimes.Clear();
            _mostRecentTriggerWindowCallback = null;
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        private Task StopRandomTriggerCallbackAsync()
        {
            return Task.Run(() =>
            {
                StopTriggerCallbacks();
            });
        }

        private void StopTriggerCallbacks()
        {
            String logMessage = "Stopping script trigger callbacks.";
            if (_triggerWindowCallbacks.Any())
            {
                lock (_triggerWindowCallbacks)
                {
                    foreach (var triggerCallbackId in _triggerWindowCallbacks.Keys)
                    {
                        SensusServiceHelper.Get().UnscheduleCallback(triggerCallbackId);
                    }

                    _triggerWindowCallbacks.Clear();
                }

                _mostRecentTriggerWindowCallback = null;
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