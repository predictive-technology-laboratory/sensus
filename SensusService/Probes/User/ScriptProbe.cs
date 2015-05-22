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

using Newtonsoft.Json;
using SensusUI.UiProperties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;

namespace SensusService.Probes.User
{
    public class ScriptProbe : Probe
    {
        private ObservableCollection<Trigger> _triggers;
        private Dictionary<Trigger, EventHandler<Tuple<Datum, Datum>>> _triggerHandler;
        private Script _script;
        private Queue<Script> _incompleteScripts;
        private bool _rerunIncompleteScripts;
        private string _scriptRerunCallbackId;
        private int _scriptRerunDelayMS;
        private int _maxScriptAgeMinutes;
        private int _numScriptsAgedOut;
        private bool _triggerRandomly;
        private string _randomTriggerCallbackId;
        private int _randomTriggerDelayMaxMinutes;
        private Random _random;

        private readonly object _locker = new object();

        public ObservableCollection<Trigger> Triggers
        {
            get { return _triggers; }
        }

        public Script Script
        {
            get { return _script; }
            set { _script = value; }
        }

        [OnOffUiProperty("Rerun Incomplete Scripts:", true, 10)]
        public bool RerunIncompleteScripts
        {
            get { return _rerunIncompleteScripts; }
            set
            {
                if (value != _rerunIncompleteScripts)
                {
                    _rerunIncompleteScripts = value;

                    if (Running)
                        if (_rerunIncompleteScripts)
                            StartScriptRerunCallbacksAsync();
                        else
                            StopScriptRerunCallbacksAsync();
                }
            }
        }

        [EntryIntegerUiProperty("Script Rerun Delay (MS):", true, 11)]
        public int ScriptRerunDelayMS
        {
            get { return _scriptRerunDelayMS; }
            set
            {
                if (value <= 1000)
                    value = 1000;
                
                if (value != _scriptRerunDelayMS)
                {
                    _scriptRerunDelayMS = value; 

                    if (_scriptRerunCallbackId != null)
                        _scriptRerunCallbackId = SensusServiceHelper.Get().RescheduleRepeatingCallback(_scriptRerunCallbackId, _scriptRerunDelayMS, _scriptRerunDelayMS);
                }
            }
        }

        [EntryIntegerUiProperty("Max. Script Age (Mins.):", true, 12)]
        public int MaxScriptAgeMinutes
        {
            get { return _maxScriptAgeMinutes; }
            set { _maxScriptAgeMinutes = value; }
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

                    if (Running)
                        if (_triggerRandomly)
                            StartRandomScriptTriggerCallbacksAsync();
                        else
                            StopRandomScriptTriggerCallbackAsync();
                }
            }
        }

        [EntryIntegerUiProperty("Random Delay Max. Mins.:", true, 14)]
        public int RandomTriggerDelayMaxMinutes
        {
            get { return _randomTriggerDelayMaxMinutes; }
            set
            {
                if (value <= 0)
                    value = 1;
                
                if (value != _randomTriggerDelayMaxMinutes)
                {        
                    bool reschedule = value < _randomTriggerDelayMaxMinutes && _randomTriggerCallbackId != null;

                    _randomTriggerDelayMaxMinutes = value; 

                    if (reschedule)
                        StartRandomScriptTriggerCallbacksAsync();
                }
            }
        }

        public Queue<Script> IncompleteScripts
        {
            get { return _incompleteScripts; }
        }

        protected override string DefaultDisplayName
        {
            get { return "User Interaction"; }
        }

        [JsonIgnore]
        public sealed override Type DatumType
        {
            get { return typeof(ScriptDatum); }
        }

        public ScriptProbe()
        {
            _triggers = new ObservableCollection<Trigger>();
            _triggerHandler = new Dictionary<Trigger, EventHandler<Tuple<Datum, Datum>>>();
            _script = new Script("Empty Script");
            _incompleteScripts = new Queue<Script>();
            _rerunIncompleteScripts = false;
            _scriptRerunCallbackId = null;
            _scriptRerunDelayMS = 60000;
            _maxScriptAgeMinutes = 10;
            _numScriptsAgedOut = 0;
            _triggerRandomly = false;
            _randomTriggerCallbackId = null;
            _randomTriggerDelayMaxMinutes = 1;
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
                                        if (!Running || prevCurrDatum.Item2 == null)
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
                                        if (prevDatum == null) // don't need to set ConditionSatisfiedLastTime = false, since it cannot be the case that it's true and prevDatum == null (we must have had a currDatum last time in order to set ConditionSatisfiedLastTime = true.
                                            return;

                                        try { currDatumValue = Convert.ToDouble(currDatumValue) - Convert.ToDouble(addedTrigger.DatumProperty.GetValue(prevDatum)); }
                                        catch (Exception ex)
                                        {
                                            SensusServiceHelper.Get().Logger.Log("Trigger error:  Failed to convert datum values to doubles:  " + ex.Message, LoggingLevel.Normal, GetType());
                                            return;
                                        }
                                    }

                                    if (addedTrigger.FireFor(currDatumValue))
                                        RunScriptAsync(_script.Copy(), prevDatum, currDatum);  // run a copy of the pristine script, since it will be filled in when run.
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

        protected override void Initialize()
        {
            base.Initialize();

            if (_script == null)
                throw new Exception("Script has not been set on " + GetType().FullName);
        }

        public override void Start()
        {
            base.Start();

            if (_rerunIncompleteScripts)
                StartScriptRerunCallbacksAsync();

            if (_triggerRandomly)
                StartRandomScriptTriggerCallbacksAsync();
        }

        private void StartScriptRerunCallbacksAsync()
        {
            new Thread(() =>
                {
                    lock (_incompleteScripts)
                    {
                        StopScriptRerunCallbacks();

                        SensusServiceHelper.Get().Logger.Log("Starting script rerun callbacks.", LoggingLevel.Normal, GetType());

                        _scriptRerunCallbackId = SensusServiceHelper.Get().ScheduleRepeatingCallback(cancellationToken =>
                            {
                                if (Running && _rerunIncompleteScripts)
                                {
                                    Script scriptToRerun = null;
                                    lock (_incompleteScripts)
                                        while (scriptToRerun == null && _incompleteScripts.Count > 0)
                                        {
                                            scriptToRerun = _incompleteScripts.Dequeue();
                                            TimeSpan scriptAge = DateTimeOffset.UtcNow - scriptToRerun.FirstRunTimestamp;
                                            if (scriptAge.TotalMinutes > _maxScriptAgeMinutes)
                                            {
                                                SensusServiceHelper.Get().Logger.Log("Script \"" + scriptToRerun.Name + "\" has aged out.", LoggingLevel.Normal, GetType());
                                                scriptToRerun = null;
                                                ++_numScriptsAgedOut;
                                            }
                                        }

                                    if (scriptToRerun != null)
                                    {
                                        ManualResetEvent scriptWait = new ManualResetEvent(false);
                                        RunScriptAsync(scriptToRerun, null, null, () => scriptWait.Set());
                                        scriptWait.WaitOne();
                                    }
                                }
                            }, "Rerun Script", _scriptRerunDelayMS, _scriptRerunDelayMS, null);  // no user notification message, since there might not be any scripts to rerun
                    }
                }).Start();
        }

        private void StartRandomScriptTriggerCallbacksAsync()
        {
            new Thread(() =>
                {
                    lock (_script)
                    {
                        StopRandomScriptTriggerCallback();

                        SensusServiceHelper.Get().Logger.Log("Starting random script trigger callbacks.", LoggingLevel.Normal, GetType());

                        #if __IOS__
                        string userNotificationMessage = "Your input is requested.";
                        #elif __ANDROID__
                        string userNotificationMessage = null;
                        #elif __WINDOWS_PHONE__
                        TODO:  Should we use a message?
                        #endif

                        _randomTriggerCallbackId = SensusServiceHelper.Get().ScheduleOneTimeCallback(RandomScriptTriggerCallback, "Randomly Rerun Script", _random.Next(_randomTriggerDelayMaxMinutes * 60000), userNotificationMessage);
                    }
                }).Start();
        }

        private void RandomScriptTriggerCallback(CancellationToken cancellationToken)
        {
            if (Running && _triggerRandomly)
            {
                ManualResetEvent scriptWait = new ManualResetEvent(false);
                RunScriptAsync(_script.Copy(), null, null, () => scriptWait.Set());
                scriptWait.WaitOne();

                StartRandomScriptTriggerCallbacksAsync();
            }
        }

        private void RunScriptAsync(Script script, Datum prevDatum, Datum currDatum)
        {
            RunScriptAsync(script, prevDatum, currDatum, () => { });
        }

        private void RunScriptAsync(Script script, Datum prevDatum, Datum currDatum, Action callback)
        {
            script.RunAsync(prevDatum, currDatum, scriptData =>
                {
                    foreach (ScriptDatum scriptDatum in scriptData)
                        if (scriptDatum != null)
                            StoreDatum(scriptDatum);

                    if (_rerunIncompleteScripts && !script.Complete)
                        lock (_incompleteScripts)
                            _incompleteScripts.Enqueue(script);

                    if (callback != null)
                        callback();
                });
        }

        public override bool TestHealth(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.TestHealth(ref error, ref warning, ref misc);

            if (Running)
            {
                lock (_incompleteScripts)
                    if (_incompleteScripts.Count > 0)
                        misc += "Listening probe is holding " + _incompleteScripts.Count + " scripts, the oldest being run first on " + _incompleteScripts.Select(s => s.FirstRunTimestamp).Min() + "." + Environment.NewLine;

                if (_numScriptsAgedOut > 0)
                    misc += _numScriptsAgedOut + " scripts have aged out." + Environment.NewLine;
            }

            return restart;
        }

        private void StopScriptRerunCallbacksAsync()
        {
            StopScriptRerunCallbacksAsync(() => { });
        }

        private void StopScriptRerunCallbacksAsync(Action callback)
        {
            new Thread(() =>
                {
                    StopScriptRerunCallbacks();

                    if(callback != null)
                        callback();

                }).Start();
        }

        private void StopScriptRerunCallbacks()
        {
            SensusServiceHelper.Get().Logger.Log("Stopping script rerun callbacks.", LoggingLevel.Normal, GetType());
            SensusServiceHelper.Get().UnscheduleOneTimeCallback(_scriptRerunCallbackId);
            _scriptRerunCallbackId = null;
        }

        private void StopRandomScriptTriggerCallbackAsync()
        {
            new Thread(() =>
                {
                    StopRandomScriptTriggerCallback();

                }).Start();
        }

        private void StopRandomScriptTriggerCallback()
        {
            SensusServiceHelper.Get().Logger.Log("Stopping random script trigger callback.", LoggingLevel.Normal, GetType());
            SensusServiceHelper.Get().UnscheduleOneTimeCallback(_randomTriggerCallbackId);
            _randomTriggerCallbackId = null;
        }

        public override void Stop()
        {
            base.Stop();

            StopScriptRerunCallbacks();
            StopRandomScriptTriggerCallback();
        }
    }
}
