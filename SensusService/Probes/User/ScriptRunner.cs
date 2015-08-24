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
using SensusService.Probes.User;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using SensusUI.UiProperties;
using System.Collections.Specialized;
using System.Threading;
using System.Linq;
using SensusUI.Inputs;

namespace SensusService.Probes.User
{
    public class ScriptRunner
    {
        private string _name;
        private ScriptProbe _probe;
        private Script _script;
        private bool _enabled;
        private int _delayMS;
        private ObservableCollection<Trigger> _triggers;
        private ObservableCollection<TimeTrigger> _timeTriggers;
        private Dictionary<Trigger, EventHandler<Tuple<Datum, Datum>>> _triggerHandler;
        private Queue<Script> _incompleteScripts;
        private bool _rerun;
        private string _rerunCallbackId;
        private int _rerunDelayMS;
        private int _maximumAgeMinutes;
        private int _numScriptsAgedOut;
//        private string _timeTriggerCallbackId;
        private Random _random;

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

        [EntryIntegerUiProperty("Delay (MS):", true, 3)]
        public int DelayMS
        {
            get { return _delayMS; }
            set { _delayMS = value; }
        }

        public ObservableCollection<Trigger> Triggers
        {
            get { return _triggers; }
        }

        public ObservableCollection<TimeTrigger> TimeTriggers
        {
            get { return _timeTriggers; }
        }

        public Queue<Script> IncompleteScripts
        {
            get { return _incompleteScripts; }
        }

        [OnOffUiProperty("Rerun Incomplete Scripts:", true, 4)]
        public bool RerunIncompletes
        {
            get { return _rerun; }
            set
            {
                if (value != _rerun)
                {
                    _rerun = value;

                    if (_probe != null && _probe.Running && _enabled && _rerun) // probe can be null when deserializing, if set after this property.
                        StartRerunCallbacksAsync();
                    else if (SensusServiceHelper.Get() != null)  // service helper is null when deserializing
                        StopRerunCallbacksAsync();
                }
            }
        }

        [EntryIntegerUiProperty("Rerun Delay (MS):", true, 5)]
        public int RerunDelayMS
        {
            get { return _rerunDelayMS; }
            set
            {
                if (value <= 1000)
                    value = 1000;

                if (value != _rerunDelayMS)
                {
                    _rerunDelayMS = value; 

                    if (_rerunCallbackId != null)
                        _rerunCallbackId = SensusServiceHelper.Get().RescheduleRepeatingCallback(_rerunCallbackId, _rerunDelayMS, _rerunDelayMS);
                }
            }
        }

        [EntryIntegerUiProperty("Maximum Age (Mins.):", true, 6)]
        public int MaximumAgeMinutes
        {
            get { return _maximumAgeMinutes; }
            set { _maximumAgeMinutes = value; }
        }

        public int NumScriptsAgedOut
        {
            get
            {
                return _numScriptsAgedOut;
            }
            set
            {
                _numScriptsAgedOut = value;
            }
        }

        #endregion

        /// <summary>
        /// For JSON.NET deserialization.
        /// </summary>
        private ScriptRunner()
        {
            _script = new Script();
            _enabled = false;
            _delayMS = 0;
            _triggers = new ObservableCollection<Trigger>();
            _timeTriggers = new ObservableCollection<TimeTrigger>();
            _triggerHandler = new Dictionary<Trigger, EventHandler<Tuple<Datum, Datum>>>();
            _incompleteScripts = new Queue<Script>();
            _rerun = false;
            _rerunCallbackId = null;
            _rerunDelayMS = 60000;
            _maximumAgeMinutes = 1;
            _numScriptsAgedOut = 0;
//            _timeTriggerCallbackId = null;
            _random = new Random();

            _triggers.CollectionChanged += (o, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                    foreach (Trigger trigger in e.NewItems)
                    {
                        // ignore duplicate triggers -- the user should delete and re-add them instead.
                        if (_triggerHandler.ContainsKey(trigger))
                            return;

                        EventHandler<Tuple<Datum, Datum>> handler = (oo, previousCurrentDatum) =>
                        {
                            // must be running and must have a current datum
                            lock (_locker)
                                if (!_probe.Running || !_enabled || previousCurrentDatum.Item2 == null)
                                {
                                    trigger.FireCriteriaMetOnPreviousCall = false;  // this covers the case when the current datum is null. for some probes, the null datum is meaningful and is emitted in order for their state to be tracked appropriately (e.g., POI probe).
                                    return;
                                }

                            Datum previousDatum = previousCurrentDatum.Item1;
                            Datum currentDatum = previousCurrentDatum.Item2;

                            // get the value that might trigger the script
                            object currentDatumValue = trigger.DatumProperty.GetValue(currentDatum);
                            if (currentDatumValue == null)
                            {
                                SensusServiceHelper.Get().Logger.Log("Trigger error:  Value of datum property " + trigger.DatumPropertyName + " was null.", LoggingLevel.Normal, GetType());
                                return;
                            }

                            // if we're triggering based on datum value changes instead of absolute values, calculate the change now
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

                            // if the trigger fires, run a copy of the script so that we can retain a pristine version of the original
                            if (trigger.FireFor(currentDatumValue))
                                RunAsync(_script.Copy(), previousDatum, currentDatum);
                        };

                        trigger.Probe.MostRecentDatumChanged += handler;
                        _triggerHandler.Add(trigger, handler);
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
            if (_rerun)
                StartRerunCallbacksAsync();

            if (_timeTriggers.Count != 0)
                StartTimeTriggerCallbacksAsync();
        }

        private void StartRerunCallbacksAsync()
        {
            new Thread(() =>
                {
                    lock (_incompleteScripts)
                    {
                        StopRerunCallbacks();

                        SensusServiceHelper.Get().Logger.Log("Starting rerun callbacks.", LoggingLevel.Normal, GetType());

                        _rerunCallbackId = SensusServiceHelper.Get().ScheduleRepeatingCallback(cancellationToken =>
                            {
                                if (_probe.Running && _enabled && _rerun)
                                {
                                    Script scriptToRerun = null;
                                    lock (_incompleteScripts)
                                        while (scriptToRerun == null && _incompleteScripts.Count > 0)
                                        {
                                            scriptToRerun = _incompleteScripts.Dequeue();                     
                                            if (scriptToRerun.Age.TotalMinutes > _maximumAgeMinutes)
                                            {
                                                SensusServiceHelper.Get().Logger.Log("Script \"" + _name + "\" has aged out.", LoggingLevel.Normal, GetType());
                                                scriptToRerun = null;
                                                ++_numScriptsAgedOut;
                                            }
                                        }

                                    // we don't need to copy the script, since we're already working with a copy of the original
                                    if (scriptToRerun != null)
                                        RunAsync(scriptToRerun);
                                }

                            }, "Rerun Script", _rerunDelayMS, _rerunDelayMS, null);  // no user notification message, since there might not be any scripts to rerun
                    }

                }).Start();
        }

        private void StartTimeTriggerCallbacksAsync()
        {
            SensusServiceHelper.Get().Logger.Log("Starting time trigger callbacks.", LoggingLevel.Normal, GetType());

            foreach (TimeTrigger timeTrigger in _timeTriggers)
            {
                    new Thread(() =>
                        {

                                lock (_locker)
                                {
                                    if (timeTrigger.RerunDaily)
                                    {
                                        timeTrigger.ID = SensusServiceHelper.Get().ScheduleRepeatingCallback(cancellationToken =>
                                        {
                                            if (_probe.Running && _enabled)
                                                RunAsync(_script.Copy());
                                            if (timeTrigger.RerunDaily)     // if rerunDaily is still set to true, reschedule another random or specific time trigger
                                            {
                                                int time = (int)TimeUntilFireMilliseconds(timeTrigger);
                                                Console.Out.WriteLine(time);
                                                SensusServiceHelper.Get().RescheduleRepeatingCallback(timeTrigger.ID, time, 0);
                                            }
                                        
                                        }, "Rerun time trigger daily", (int)TimeUntilFireMilliseconds(timeTrigger), 0, null);
                                    }
                                    else
                                    {
                                        timeTrigger.ID = SensusServiceHelper.Get().ScheduleOneTimeCallback(cancellationToken =>
                                            {
                                                if (_probe.Running && _enabled)
                                                    RunAsync(_script.Copy());
                                        }, "Rerun time trigger daily", (int)TimeUntilFireMilliseconds(timeTrigger), null);
                                    }
                                }
                            
                        }).Start();
            }
        }

        private double TimeUntilFireMilliseconds(TimeTrigger trigger)
        {
            TimeSpan fullDay = new TimeSpan(24, 00, 00);
            TimeSpan timeNow = TimeSpan.Parse(DateTime.Now.ToString("HH:mm"));
            TimeSpan timeUntilFire = new TimeSpan();
            TimeSpan timeFire;

            int hour;
            int minute;

            hour = _random.Next(trigger.StartTime.Hours, trigger.EndTime.Hours + 1);
            if (trigger.StartTime.Hours == trigger.EndTime.Hours)
                minute = _random.Next(trigger.StartTime.Minutes, trigger.EndTime.Minutes);
            else
            {
                if (trigger.StartTime.Hours == hour)
                    minute = _random.Next(trigger.StartTime.Minutes, 60);
                else if (trigger.EndTime.Hours == hour)
                    minute = _random.Next(0, trigger.EndTime.Minutes);
                else
                    minute = _random.Next(0, 60);
            }

            timeFire = new TimeSpan(hour, minute, 0);
            Console.Out.WriteLine(timeFire);                    // debug

            if (timeFire <= timeNow)
                timeUntilFire = (timeFire - timeNow) + fullDay;
            else
                timeUntilFire = (timeFire - timeNow);

            Console.Out.WriteLine(timeUntilFire);

            return timeUntilFire.TotalMilliseconds;
        }

        private void RunAsync(Script script)
        {
            RunAsync(script, null, null, null);
        }

        private void RunAsync(Script script, Action callback)
        {
            RunAsync(script, null, null, callback);
        }

        private void RunAsync(Script script, Datum previousDatum, Datum currentDatum)
        {
            RunAsync(script, previousDatum, currentDatum, null);
        }

        private void RunAsync(Script script, Datum previousDatum, Datum currentDatum, Action callback)
        {
            #if __IOS__
            string userNotificationMessage = "Your input is requested.";
            #elif __ANDROID__
            string userNotificationMessage = null;
            #elif WINDOWS_PHONE
            string userNotificationMessage = null; // TODO:  Should we use a message?
            #else
            #error "Unrecognized platform."
            #endif

            Action<CancellationToken> runAction = cancellationToken =>
            {
                SensusServiceHelper.Get().Logger.Log("Running \"" + _name + "\".", LoggingLevel.Normal, typeof(Script));

                bool isRerun = true;

                if (script.FirstRunTimestamp == DateTimeOffset.MinValue)
                {
                    script.FirstRunTimestamp = DateTimeOffset.UtcNow;
                    isRerun = false;
                }

                // this method can be called with previous / current datum values (e.g., when the script is first triggered. it 
                // can also be called without previous / current datum values (e.g., when triggering randomly or rerunning). if
                // we have such values, set them on the script.

                if (previousDatum != null)
                    script.PreviousDatum = previousDatum;

                if (currentDatum != null)
                    script.CurrentDatum = currentDatum;

                ManualResetEvent inputWait = new ManualResetEvent(false);

                SensusServiceHelper.Get().PromptForInputsAsync(script.CurrentDatum, isRerun, script.FirstRunTimestamp, script.InputGroups, inputGroups =>
                    {
                        if (inputGroups != null)
                            foreach (InputGroup inputGroup in inputGroups)
                                foreach (Input input in inputGroup.Inputs)
                                    if (input.ShouldBeStored && input.Complete)
                                    {
                                        _probe.StoreDatum(new ScriptDatum(DateTimeOffset.UtcNow, input.GroupId, input.Id, input.Value, script.CurrentDatum == null ? null : script.CurrentDatum.Id));

                                        // once inputs are stored, they should be stored again, nor should the user be able to modify them
                                        input.ShouldBeStored = false;
                                        Xamarin.Forms.Device.BeginInvokeOnMainThread(() => input.Enabled = false);
                                    }

                        inputWait.Set();
                    });

                inputWait.WaitOne();

                SensusServiceHelper.Get().Logger.Log("\"" + _name + "\" has finished running.", LoggingLevel.Normal, typeof(Script));

                if (_rerun && !script.Complete)
                    lock (_incompleteScripts)
                        _incompleteScripts.Enqueue(script);

                if (callback != null)
                    callback();
            };

            if (_delayMS > 0)
                SensusServiceHelper.Get().ScheduleOneTimeCallback(runAction, "Run Script", _delayMS, userNotificationMessage);
            else
                new Thread(() =>
                    {
                        runAction(default(CancellationToken));

                    }).Start();
        }

        public bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = false;

            lock (_incompleteScripts)
                if (_incompleteScripts.Count > 0)
                    misc += "Script runner \"" + _name + "\" is holding " + _incompleteScripts.Count + " copies, the oldest being run first on " + _incompleteScripts.Select(s => s.FirstRunTimestamp).Min() + "." + Environment.NewLine;

            if (_numScriptsAgedOut > 0)
                misc += _numScriptsAgedOut + " \"" + _name + "\" scripts have aged out." + Environment.NewLine;
            
            return restart;
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        private void StopRerunCallbacksAsync()
        {
            new Thread(() =>
                {
                    StopRerunCallbacks();
                }).Start();
        }

        private void StopRerunCallbacks()
        {
            SensusServiceHelper.Get().Logger.Log("Stopping rerun callbacks.", LoggingLevel.Normal, GetType());
            SensusServiceHelper.Get().UnscheduleOneTimeCallback(_rerunCallbackId);
            _rerunCallbackId = null;
        }

        private void StopTimeTriggerCallbackAsync()
        {
            new Thread(() =>
                {
                    StopTimeTriggerCallbacks();
                }).Start();
        }

        private void StopTimeTriggerCallbacks()
        {
            SensusServiceHelper.Get().Logger.Log("Stopping random trigger callbacks.", LoggingLevel.Normal, GetType());
            foreach (TimeTrigger trigger in _timeTriggers)
            {
                SensusServiceHelper.Get().UnscheduleOneTimeCallback(trigger.ID);
                SensusServiceHelper.Get().UnscheduleRepeatingCallback(trigger.ID);
            }
//            _timeTriggerCallbackId = null;
        }

        public void Stop()
        {
            StopRerunCallbacks();
            StopTimeTriggerCallbacks();
        }
    }
}