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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using SensusUI.UiProperties;

namespace SensusService.Probes.User
{
    public class Script
    {
        #region static members
        private static JsonSerializerSettings JSON_SERIALIZER_SETTINGS = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };
        #endregion

        private ScriptProbe _probe;
        private string _id;
        private int _hashCode;
        private string _name;
        private bool _enabled;
        private ObservableCollection<Trigger> _triggers;
        private Dictionary<Trigger, EventHandler<Tuple<Datum, Datum>>> _triggerHandler;
        private int _delayMS;
        private ObservableCollection<Prompt> _prompts;
        private DateTimeOffset _firstRunTimestamp;
        private Datum _previousDatum;
        private Datum _currentDatum;
        private Queue<Script> _incompletes;
        private bool _rerunIncompletes;
        private string _rerunCallbackId;
        private int _rerunDelayMS;
        private int _maximumAgeMinutes;
        private bool _triggerRandomly;
        private string _randomTriggerCallbackId;
        private int _maximumRandomTriggerDelayMinutes;
        private Random _random;

        private readonly object _locker = new object();

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

        public string Id
        {
            get { return _id; }
            set
            {
                _id = value;

                if (_id == null)
                    _hashCode = base.GetHashCode();
                else
                    _hashCode = _id.GetHashCode();
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
                _enabled = value;

                if (_probe.Running && _enabled)
                    Start();
            }
        }

        public ObservableCollection<Trigger> Triggers
        {
            get { return _triggers; }
        }

        [EntryIntegerUiProperty("Delay (MS):", true, 3)]
        public int DelayMS
        {
            get { return _delayMS; }
            set { _delayMS = value; }
        }

        public ObservableCollection<Prompt> Prompts
        {
            get { return _prompts; }
        }

        public DateTimeOffset FirstRunTimestamp
        {
            get { return _firstRunTimestamp; }
            set { _firstRunTimestamp = value; }
        }

        public Datum PreviousDatum
        {
            get { return _previousDatum; }
            set { _previousDatum = value; }
        }

        public Datum CurrentDatum
        {
            get { return _currentDatum; }
            set { _currentDatum = value; }
        }

        public Queue<Script> Incompletes
        {
            get { return _incompletes; }
        }

        [OnOffUiProperty("Rerun Incompletes:", true, 10)]
        public bool RerunIncompletes
        {
            get { return _rerunIncompletes; }
            set
            {
                if (value != _rerunIncompletes)
                {
                    _rerunIncompletes = value;

                    if (_probe.Running && _enabled && _rerunIncompletes)
                        StartRerunCallbacksAsync();
                    else
                        StopRerunCallbacksAsync();
                }
            }
        }

        [EntryIntegerUiProperty("Rerun Delay (MS):", true, 11)]
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

        [EntryIntegerUiProperty("Maximum Age (Mins.):", true, 12)]
        public int MaximumAgeMinutes
        {
            get { return _maximumAgeMinutes; }
            set { _maximumAgeMinutes = value; }
        }

        [OnOffUiProperty("Trigger Randomly:", true, 13)]
        public bool TriggerRandomly
        {
            get { return _triggerRandomly; }
            set
            {
                if (value != _triggerRandomly)
                {
                    _triggerRandomly = value;

                    if (_probe.Running && _enabled && _triggerRandomly)
                        StartRandomTriggerCallbacksAsync();
                    else
                        StopRandomTriggerCallbackAsync();
                }
            }
        }

        [EntryIntegerUiProperty("Maximum Random Delay (Mins.):", true, 14)]
        public int MaximumRandomTriggerDelayMinutes
        {
            get { return _maximumRandomTriggerDelayMinutes; }
            set
            {
                if (value <= 0)
                    value = 1;

                if (value != _maximumRandomTriggerDelayMinutes)
                {        
                    bool reschedule = value < _maximumRandomTriggerDelayMinutes && _randomTriggerCallbackId != null;

                    _maximumRandomTriggerDelayMinutes = value; 

                    if (reschedule)
                        StartRandomTriggerCallbacksAsync();
                }
            }
        }

        [JsonIgnore]
        public bool Complete
        {
            get { return _prompts.Count == 0 || _prompts.All(p => p.Complete); }
        }

        [JsonIgnore]
        public TimeSpan Age
        {
            get { return DateTimeOffset.UtcNow - _firstRunTimestamp; }
        }

        /// <summary>
        /// Constructor for JSON deserialization.
        /// </summary>
        private Script()
        {            
            _id = Guid.NewGuid().ToString();
            _hashCode = _id.GetHashCode();
            _triggers = new ObservableCollection<Trigger>();
            _triggerHandler = new Dictionary<Trigger, EventHandler<Tuple<Datum, Datum>>>();
            _delayMS = 0;
            _prompts = new ObservableCollection<Prompt>();
            _incompletes = new Queue<Script>();
            _rerunIncompletes = false;
            _rerunCallbackId = null;
            _rerunDelayMS = 60000;
            _maximumAgeMinutes = 10;
            _triggerRandomly = false;
            _randomTriggerCallbackId = null;
            _maximumRandomTriggerDelayMinutes = 1;
            _random = new Random();

            _triggers.CollectionChanged += (o, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                    foreach (Trigger addedTrigger in e.NewItems)
                    {
                        // ignore duplicate triggers -- the user should delete and re-add them instead.
                        if (_triggerHandler.ContainsKey(addedTrigger))
                            return;

                        EventHandler<Tuple<Datum, Datum>> handler = (oo, prevCurrDatum) =>
                        {
                            // must be running and must have a current datum
                            lock (_locker)
                                if (!_probe.Running || !_enabled || prevCurrDatum.Item2 == null)
                                {
                                    addedTrigger.ConditionSatisfiedLastTime = false;  // this covers the case when the current datum is null. some probes need to emit a null datum in order for their state to be tracked appropriately (e.g., POI probe).
                                    return;
                                }

                            Datum prevDatum = prevCurrDatum.Item1;
                            Datum currDatum = prevCurrDatum.Item2;

                            // get the value that might trigger the script
                            object currDatumValue = addedTrigger.DatumProperty.GetValue(currDatum);
                            if (currDatumValue == null)
                            {
                                SensusServiceHelper.Get().Logger.Log("Trigger error:  Value of datum property " + addedTrigger.DatumPropertyName + " was null.", LoggingLevel.Normal, GetType());
                                return;
                            }

                            // if we're triggering based on datum value changes instead of absolute values, calculate the change now
                            if (addedTrigger.Change)
                            {
                                // don't need to set ConditionSatisfiedLastTime = false here, since it cannot be the case that it's true and prevDatum == null (we must have had a currDatum last time in order to set ConditionSatisfiedLastTime = true.
                                if (prevDatum == null)
                                    return;

                                try
                                {
                                    currDatumValue = Convert.ToDouble(currDatumValue) - Convert.ToDouble(addedTrigger.DatumProperty.GetValue(prevDatum));
                                }
                                catch (Exception ex)
                                {
                                    SensusServiceHelper.Get().Logger.Log("Trigger error:  Failed to convert datum values to doubles:  " + ex.Message, LoggingLevel.Normal, GetType());
                                    return;
                                }
                            }

                            if (addedTrigger.FireFor(currDatumValue))
                                RunAsync(prevDatum, currDatum, null);
                        };

                        addedTrigger.Probe.MostRecentDatumChanged += handler;
                        _triggerHandler.Add(addedTrigger, handler);
                    }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                    foreach (Trigger removedTrigger in e.OldItems)
                        if (_triggerHandler.ContainsKey(removedTrigger))
                        {
                            removedTrigger.Probe.MostRecentDatumChanged -= _triggerHandler[removedTrigger];
                            _triggerHandler.Remove(removedTrigger);
                        }
            };
        }

        public Script(string name, int delayMS, ScriptProbe probe)
            : this()
        {
            _name = name;
            _delayMS = delayMS;
            _probe = probe;
        }

        public void Start()
        {
            if (_rerunIncompletes)
                StartRerunCallbacksAsync();
            
            if (_triggerRandomly)
                StartRandomTriggerCallbacksAsync();
        }

        private void StartRerunCallbacksAsync()
        {
            new Thread(() =>
                {
                    lock (_incompletes)
                    {
                        StopRerunCallbacks();

                        SensusServiceHelper.Get().Logger.Log("Starting rerun callbacks.", LoggingLevel.Normal, GetType());

                        _rerunCallbackId = SensusServiceHelper.Get().ScheduleRepeatingCallback(cancellationToken =>
                            {
                                if (_probe.Running && _enabled && _rerunIncompletes)
                                {
                                    Script scriptToRerun = null;
                                    lock (_incompletes)
                                        while (scriptToRerun == null && _incompletes.Count > 0)
                                        {
                                            scriptToRerun = _incompletes.Dequeue();                     
                                            if (scriptToRerun.Age.TotalMinutes > scriptToRerun.MaximumAgeMinutes)
                                            {
                                                SensusServiceHelper.Get().Logger.Log("Script \"" + scriptToRerun.Name + "\" has aged out.", LoggingLevel.Normal, GetType());
                                                scriptToRerun = null;
                                                ++_probe.NumScriptsAgedOut;
                                            }
                                        }

                                    if (scriptToRerun != null)
                                        scriptToRerun.RunAsync(null, null, null);
                                }

                            }, "Rerun Script", _rerunDelayMS, _rerunDelayMS, null);  // no user notification message, since there might not be any scripts to rerun
                    }

                }).Start();
        }

        private void StartRandomTriggerCallbacksAsync()
        {
            new Thread(() =>
                {
                    lock (_locker)
                    {
                        StopRandomTriggerCallbacks();

                        SensusServiceHelper.Get().Logger.Log("Starting random script trigger callbacks.", LoggingLevel.Normal, GetType());

                        #if __IOS__
                        string userNotificationMessage = "Your input is requested.";
                        #elif __ANDROID__
                        string userNotificationMessage = null;
                        #elif __WINDOWS_PHONE__
                        TODO:  Should we use a message?
                        #endif

                        _randomTriggerCallbackId = SensusServiceHelper.Get().ScheduleOneTimeCallback(cancellationToken =>
                            {
                                if (_probe.Running && _enabled && _triggerRandomly)
                                    RunAsync(null, null, StartRandomTriggerCallbacksAsync);
                            }
                            , "Randomly Rerun", _random.Next(_maximumRandomTriggerDelayMinutes * 60000), userNotificationMessage);
                    }
                }).Start();
        }

        private void RunAsync(Datum previousDatum, Datum currentDatum, Action callback)
        {
            Script script = Copy();

            #if __IOS__
            string userNotificationMessage = "Your input is requested.";
            #elif __ANDROID__
            string userNotificationMessage = null;
            #elif __WINDOWS_PHONE__
            TODO:  Should we use a message?
            #endif

            Action<CancellationToken> runAction = cancellationToken =>
            {
                SensusServiceHelper.Get().Logger.Log("Running \"" + script.Name + "\".", LoggingLevel.Normal, typeof(Script));

                bool isRerun = true;

                if (script.FirstRunTimestamp == DateTimeOffset.MinValue)
                {
                    script.FirstRunTimestamp = DateTimeOffset.UtcNow;
                    isRerun = false;
                }

                if (previousDatum != null)
                    script.PreviousDatum = previousDatum;

                if (currentDatum != null)
                    script.CurrentDatum = currentDatum;

                foreach (Prompt prompt in script.Prompts)
                    if (!prompt.Complete)
                    {
                        ManualResetEvent datumWait = new ManualResetEvent(false);

                        prompt.RunAsync(script.PreviousDatum, script.CurrentDatum, isRerun, script.FirstRunTimestamp, datum =>
                            {
                                if (datum != null)
                                    script.Probe.StoreDatum(datum);

                                datumWait.Set();
                            });

                        datumWait.WaitOne();
                    }

                SensusServiceHelper.Get().Logger.Log("\"" + script.Name + "\" has finished running.", LoggingLevel.Normal, typeof(Script));

                if (script.RerunIncompletes && !script.Complete)
                    lock (script.Incompletes)
                        script.Incompletes.Enqueue(script);

                if (callback != null)
                    callback();
            };

            if (script.DelayMS > 0)
                SensusServiceHelper.Get().ScheduleOneTimeCallback(runAction, "Run Script", script.DelayMS, userNotificationMessage);
            else
                new Thread(() =>
                    {
                        runAction(default(CancellationToken));

                    }).Start();
        }

        public bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = false;

            lock (_incompletes)
                if (_incompletes.Count > 0)
                    misc += "Script is holding " + _incompletes.Count + " copies, the oldest being run first on " + _incompletes.Select(s => s.FirstRunTimestamp).Min() + "." + Environment.NewLine;

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

        private void StopRandomTriggerCallbackAsync()
        {
            new Thread(() =>
                {
                    StopRandomTriggerCallbacks();

                }).Start();
        }

        private void StopRandomTriggerCallbacks()
        {
            SensusServiceHelper.Get().Logger.Log("Stopping random trigger callback.", LoggingLevel.Normal, GetType());
            SensusServiceHelper.Get().UnscheduleOneTimeCallback(_randomTriggerCallbackId);
            _randomTriggerCallbackId = null;
        }

        public void Stop()
        {
            StopRerunCallbacks();
            StopRandomTriggerCallbacks();
        }

        public Script Copy()
        {
            return JsonConvert.DeserializeObject<Script>(JsonConvert.SerializeObject(this, JSON_SERIALIZER_SETTINGS));
        }

        public override bool Equals(object obj)
        {
            Script s = obj as Script;

            return s != null && _id == s._id;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}