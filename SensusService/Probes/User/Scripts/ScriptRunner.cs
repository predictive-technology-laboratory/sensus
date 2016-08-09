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
        private int _maximumAgeMinutes;
        private List<Tuple<DateTime, DateTime>> _randomTriggerWindows;
        private string _randomTriggerCallbackId;
        private Random _random;
        private List<DateTime> _runTimes;
        private List<DateTime> _completionTimes;
        private bool _oneShot;
        private bool _runOnStart;
        private bool _displayProgress;

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

        [EntryIntegerUiProperty("Maximum Age (Mins.):", true, 7)]
        public int MaximumAgeMinutes
        {
            get { return _maximumAgeMinutes; }
            set { _maximumAgeMinutes = value; }
        }

        [EntryStringUiProperty("Random Windows:", true, 8)]
        public string RandomTriggerWindows
        {
            get
            {
                if (_randomTriggerWindows.Count == 0)
                    return "";
                else
                    return string.Concat(_randomTriggerWindows.Select((window, index) => (index == 0 ? "" : ", ") +
                            (
                                window.Item1 == window.Item2 ? window.Item1.Hour + ":" + window.Item1.Minute.ToString().PadLeft(2, '0') :
                                window.Item1.Hour + ":" + window.Item1.Minute.ToString().PadLeft(2, '0') + "-" + window.Item2.Hour + ":" + window.Item2.Minute.ToString().PadLeft(2, '0')
                            )));
            }
            set
            {
                if (value == RandomTriggerWindows)
                    return;

                _randomTriggerWindows.Clear();

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

                        _randomTriggerWindows.Add(new Tuple<DateTime, DateTime>(start, end));
                    }
                }
                catch (Exception)
                {
                }

                // sort windows by increasing hour and minute of the window start (day, month, and year are irrelevant)
                _randomTriggerWindows.Sort((window1, window2) =>
                    {
                        int hourComparison = window1.Item1.Hour.CompareTo(window2.Item1.Hour);

                        if (hourComparison != 0)
                            return hourComparison;
                        else
                            return window1.Item1.Minute.CompareTo(window2.Item1.Minute);
                    });

                if (_probe != null && _probe.Running && _enabled && _randomTriggerWindows.Count > 0)  // probe can be null during deserialization if this property is set first
                    StartRandomTriggerCallbacksAsync();
                else if (SensusServiceHelper.Get() != null)  // service helper can be null when deserializing
                    StopRandomTriggerCallbackAsync();
            }
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
            _maximumAgeMinutes = 10;
            _randomTriggerWindows = new List<Tuple<DateTime, DateTime>>();
            _randomTriggerCallbackId = null;
            _random = new Random();
            _runTimes = new List<DateTime>();
            _completionTimes = new List<DateTime>();
            _oneShot = false;
            _runOnStart = false;
            _displayProgress = true;

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
            if (_randomTriggerWindows.Count > 0)
                StartRandomTriggerCallbacksAsync();

            // use the async version below for a couple reasons. first, we're in a non-async method and we want to ensure
            // that the script won't be running on the UI thread. second, from the caller's perspective the prompt should 
            // not need to finish running in order for the runner to be considered started.
            if (_runOnStart)
                RunAsync(_script.Copy());
        }

        private void StartRandomTriggerCallbacksAsync()
        {
            new Thread(() =>
            {
                StartRandomTriggerCallbacks();

            }).Start();
        }

        private void StartRandomTriggerCallbacks()
        {
            lock (_locker)
            {
                if (_randomTriggerWindows.Count == 0)
                    return;

                StopRandomTriggerCallbacks();

                SensusServiceHelper.Get().Logger.Log("Starting random script trigger callbacks.", LoggingLevel.Normal, GetType());

#if __IOS__
                string userNotificationMessage = "Please open to provide input.";
#elif __ANDROID__
                string userNotificationMessage = null;
#elif WINDOWS_PHONE
                string userNotificationMessage = null; // TODO:  Should we use a message?
#else
#error "Unrecognized platform."
#endif

                // find next future trigger window, ignoring month, day, and year of windows. the windows are already sorted.
                DateTime triggerWindowStart = default(DateTime);
                DateTime triggerWindowEnd = default(DateTime);
                DateTime now = DateTime.Now;
                bool foundTriggerWindow = false;
                foreach (Tuple<DateTime, DateTime> randomTriggerWindow in _randomTriggerWindows)
                {
                    if (randomTriggerWindow.Item1.Hour > now.Hour || (randomTriggerWindow.Item1.Hour == now.Hour && randomTriggerWindow.Item1.Minute > now.Minute))
                    {
                        triggerWindowStart = new DateTime(now.Year, now.Month, now.Day, randomTriggerWindow.Item1.Hour, randomTriggerWindow.Item1.Minute, 0);
                        triggerWindowEnd = new DateTime(now.Year, now.Month, now.Day, randomTriggerWindow.Item2.Hour, randomTriggerWindow.Item2.Minute, 0);
                        foundTriggerWindow = true;
                        break;
                    }
                }

                // if there were no future trigger windows, skip to the next day and use the first trigger window
                if (!foundTriggerWindow)
                {
                    Tuple<DateTime, DateTime> firstRandomTriggerWindow = _randomTriggerWindows.First();
                    triggerWindowStart = new DateTime(now.Year, now.Month, now.Day, firstRandomTriggerWindow.Item1.Hour, firstRandomTriggerWindow.Item1.Minute, 0).AddDays(1);
                    triggerWindowEnd = new DateTime(now.Year, now.Month, now.Day, firstRandomTriggerWindow.Item2.Hour, firstRandomTriggerWindow.Item2.Minute, 0).AddDays(1);
                }

                // schedule callback for random offset into trigger window
                DateTime triggerTime = triggerWindowStart.AddSeconds(_random.NextDouble() * (triggerWindowEnd - triggerWindowStart).TotalSeconds);
                int triggerDelayMS = (int)(triggerTime - now).TotalMilliseconds;

                ScheduledCallback callback = new ScheduledCallback((callbackId, cancellationToken, letDeviceSleepCallback) =>
                {
                    return Task.Run(() =>
                    {
                        // if the probe is still running and the runner is enabled, run a copy of the script so that we can retain a pristine version of the original.
                        // also, when the script prompts display let the caller know that it's okay for the device to sleep.
                        if (_probe.Running && _enabled)
                        {
                            Run(_script.Copy());

                            // establish the next random trigger callback
                            StartRandomTriggerCallbacks();
                        }
                    });

                }, "Trigger Randomly", null, userNotificationMessage);

                _randomTriggerCallbackId = SensusServiceHelper.Get().ScheduleOneTimeCallback(callback, triggerDelayMS);
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
            lock (_runTimes)
            {
                // do not run a one-shot script if it has already been run
                if (_oneShot && _runTimes.Count > 0)
                {
                    SensusServiceHelper.Get().Logger.Log("Not running one-shot script multiple times.", LoggingLevel.Normal, GetType());
                    return;
                }

                // track participation by noting all times that the script was first run.
                _runTimes.Add(DateTime.Now);
                _runTimes.RemoveAll(r => r < _probe.Protocol.ParticipationHorizon);
            }

            SensusServiceHelper.Get().Logger.Log("Running \"" + _name + "\".", LoggingLevel.Normal, GetType());

            DateTimeOffset runTime = DateTimeOffset.UtcNow;

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

                await _probe.StoreDatumAsync(new ScriptRunDatum(runTime, _script.Id, _name, script.Id, script.CurrentDatum?.Id, latitude, longitude, locationTimestamp), default(CancellationToken));
            });

            // this method can be called with previous / current datum values (e.g., when the script is first triggered). it 
            // can also be called without previous / current datum values (e.g., when triggering randomly). if
            // we have such values, set them on the script.

            if (previousDatum != null)
                script.PreviousDatum = previousDatum;

            if (currentDatum != null)
                script.CurrentDatum = currentDatum;

            script.RunTimestamp = runTime;

            lock (_probe.Protocol.ScriptsToRun)
            {
                _probe.Protocol.ScriptsToRun.Add(script);
            }
        }

        public bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            return false;
        }

        public void Reset()
        {
            _randomTriggerCallbackId = null;
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
                    StopRandomTriggerCallbacks();

                }).Start();
        }

        private void StopRandomTriggerCallbacks()
        {
            SensusServiceHelper.Get().Logger.Log("Stopping random trigger callbacks.", LoggingLevel.Normal, GetType());
            SensusServiceHelper.Get().UnscheduleCallback(_randomTriggerCallbackId);
            _randomTriggerCallbackId = null;
        }

        public void Stop()
        {
            StopRandomTriggerCallbacks();
        }
    }
}