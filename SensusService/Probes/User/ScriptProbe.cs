#region copyright
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
#endregion

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
        private Thread _scriptRerunThread;
        private bool _stopScriptRerunThread;
        private int _scriptRerunDelayMS;
        private int _maxScriptAgeMinutes;
        private int _numScriptsAgedOut;
        private bool _triggerRandomly;
        private Thread _randomTriggerThread;
        private bool _stopRandomTriggerThread;
        private int _randomTriggerDelayMaxMinutes;

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
                            StartScriptRerunThreadAsync();
                        else
                            StopScriptRerunThreadAsync();
                }
            }
        }

        [EntryIntegerUiProperty("Script Rerun Delay (MS):", true, 11)]
        public int ScriptRerunDelayMS
        {
            get { return _scriptRerunDelayMS; }
            set { _scriptRerunDelayMS = value; }
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
                            StartRandomScriptTriggerThreadAsync();
                        else
                            StopRandomScriptTriggerThreadAsync();
                }
            }
        }

        [EntryIntegerUiProperty("Random Delay Max. Mins.:", true, 14)]
        public int RandomTriggerDelayMaxMinutes
        {
            get { return _randomTriggerDelayMaxMinutes; }
            set { _randomTriggerDelayMaxMinutes = value; }
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
            _incompleteScripts = new Queue<Script>();
            _rerunIncompleteScripts = false;
            _scriptRerunDelayMS = 60000;
            _stopScriptRerunThread = true;
            _maxScriptAgeMinutes = 10;
            _numScriptsAgedOut = 0;
            _triggerRandomly = false;
            _randomTriggerDelayMaxMinutes = 1;

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
                                    lock (this)
                                        if (!Running || prevCurrDatum.Item2 == null)
                                            return;

                                    Datum prevDatum = prevCurrDatum.Item1;
                                    Datum currDatum = prevCurrDatum.Item2;

                                    // get the object that might trigger the script
                                    object datumValue = addedTrigger.DatumProperty.GetValue(currDatum);
                                    if (datumValue == null)
                                    {
                                        SensusServiceHelper.Get().Logger.Log("Trigger error:  Value of datum property " + addedTrigger.DatumPropertyName + " was null.", LoggingLevel.Normal);
                                        return;
                                    }

                                    // if we're triggering based on datum value changes instead of absolute values, calculate the change now
                                    if (addedTrigger.Change)
                                    {
                                        if (prevDatum == null)
                                            return;

                                        try { datumValue = Convert.ToDouble(datumValue) - Convert.ToDouble(addedTrigger.DatumProperty.GetValue(prevDatum)); }
                                        catch (Exception ex)
                                        {
                                            SensusServiceHelper.Get().Logger.Log("Trigger error:  Failed to convert datum values to doubles:  " + ex.Message, LoggingLevel.Normal);
                                            return;
                                        }
                                    }

                                    if (addedTrigger.FireFor(datumValue))
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
                StartScriptRerunThreadAsync();

            if (_triggerRandomly)
                StartRandomScriptTriggerThreadAsync();
        }

        private void StartScriptRerunThreadAsync()
        {
            StopScriptRerunThread();

            SensusServiceHelper.Get().Logger.Log("Starting script rerun thread.", LoggingLevel.Normal);

            _scriptRerunThread = new Thread(() =>
                {
                    _stopScriptRerunThread = false;

                    while (!_stopScriptRerunThread)
                    {
                        int msToSleep = _scriptRerunDelayMS;
                        while (!_stopScriptRerunThread && msToSleep > 0)
                        {
                            Thread.Sleep(1000);
                            msToSleep -= 1000;
                        }

                        if (!_stopScriptRerunThread)
                        {
                            Script scriptToRerun = null;
                            lock (_incompleteScripts)
                                while (scriptToRerun == null && _incompleteScripts.Count > 0)
                                {
                                    scriptToRerun = _incompleteScripts.Dequeue();
                                    TimeSpan scriptAge = DateTimeOffset.UtcNow - scriptToRerun.FirstRunTimestamp;
                                    if (scriptAge.TotalMinutes > _maxScriptAgeMinutes)
                                    {
                                        SensusServiceHelper.Get().Logger.Log("Script \"" + scriptToRerun.Name + "\" has aged out.", LoggingLevel.Normal);
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
                    }

                    SensusServiceHelper.Get().Logger.Log("Script rerun thread has exited its while-loop.", LoggingLevel.Normal);
                });

            _scriptRerunThread.Start();
        }

        private void StartRandomScriptTriggerThreadAsync()
        {
            new Thread(() =>
                {
                    StopRandomScriptTriggerThread();

                    SensusServiceHelper.Get().Logger.Log("Starting random script trigger thread.", LoggingLevel.Normal);

                    _randomTriggerThread = new Thread(() =>
                        {
                            _stopRandomTriggerThread = false;
                            Random random = new Random();

                            while (!_stopRandomTriggerThread)
                            {
                                int msToSleep = random.Next(_randomTriggerDelayMaxMinutes * 60 * 1000);
                                while (!_stopRandomTriggerThread && msToSleep > 0)
                                {
                                    Thread.Sleep(1000);
                                    msToSleep -= 1000;
                                }

                                if (!_stopRandomTriggerThread)
                                {
                                    ManualResetEvent scriptWait = new ManualResetEvent(false);
                                    RunScriptAsync(_script.Copy(), null, null, () => scriptWait.Set());
                                    scriptWait.WaitOne();
                                }
                            }

                            SensusServiceHelper.Get().Logger.Log("Random script trigger thread has exited its while-loop.", LoggingLevel.Normal);
                        });

                    _randomTriggerThread.Start();

                }).Start();
        }

        private void RunScriptAsync(Script script, Datum prevDatum, Datum currDatum)
        {
            RunScriptAsync(script, prevDatum, currDatum, () => { });
        }

        private void RunScriptAsync(Script script, Datum prevDatum, Datum currDatum, Action callback)
        {
            new Thread(() =>
                {
                    script.RunAsync(prevDatum, currDatum, scriptData =>
                        {
                            foreach (ScriptDatum scriptDatum in scriptData)
                                if (scriptDatum != null)
                                {
                                    scriptDatum.ProbeType = GetType().FullName;
                                    StoreDatum(scriptDatum);
                                }

                            if (_rerunIncompleteScripts && !script.Complete)
                                lock (_incompleteScripts)
                                    _incompleteScripts.Enqueue(script);

                            callback();
                        });
                }).Start();
        }

        public override bool Ping(ref string error, ref string warning, ref string misc)
        {
            bool restart = base.Ping(ref error, ref warning, ref misc);

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

        private void StopScriptRerunThreadAsync()
        {
            StopScriptRerunThreadAsync(() => { });
        }

        private void StopScriptRerunThreadAsync(Action callback)
        {
            new Thread(() =>
                {
                    StopScriptRerunThread();
                    callback();

                }).Start();
        }

        private void StopScriptRerunThread()
        {
            if (_scriptRerunThread != null)
            {
                SensusServiceHelper.Get().Logger.Log("Stopping script rerun thread.", LoggingLevel.Normal);
                _stopScriptRerunThread = true;
                _scriptRerunThread.Join();
            }
        }

        private void StopRandomScriptTriggerThreadAsync()
        {
            StopRandomScriptTriggerThreadAsync(() => { });
        }

        private void StopRandomScriptTriggerThreadAsync(Action callback)
        {
            new Thread(() =>
                {
                    StopRandomScriptTriggerThread();
                    callback();

                }).Start();
        }

        private void StopRandomScriptTriggerThread()
        {
            if (_randomTriggerThread != null)
            {
                SensusServiceHelper.Get().Logger.Log("Stopping random script trigger thread.", LoggingLevel.Normal);
                _stopRandomTriggerThread = true;
                _randomTriggerThread.Join();
            }
        }

        public override void Stop()
        {
            base.Stop();

            StopScriptRerunThread();
            StopRandomScriptTriggerThread();
        }
    }
}
