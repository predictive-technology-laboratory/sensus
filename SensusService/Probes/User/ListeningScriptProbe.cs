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
using System.Threading;
using System.Threading.Tasks;

namespace SensusService.Probes.User
{
    public class ListeningScriptProbe : ListeningProbe, IScriptProbe
    {
        private ObservableCollection<Trigger> _triggers;
        private Dictionary<Trigger, EventHandler<Tuple<Datum, Datum>>> _triggerHandler;
        private bool _listening;
        private Script _script;
        private Queue<Script> _incompleteScripts;
        private bool _rerunIncompleteScripts;
        private Task _scriptRerunTask;
        private bool _stopScriptRerunTask;
        private int _scriptRerunDelayMS;
        private int _maxScriptAgeMinutes;

        public ObservableCollection<Trigger> Triggers
        {
            get { return _triggers; }
        }

        [ReadTextFileUiProperty("Script:", true, 3, "Load", "Select Script (.json)")]
        [JsonIgnore]
        public string ScriptContent
        {
            get { return _script == null ? null : _script.Name; }
            set
            {
                try
                {
                    _script = Script.FromJSON(value);
                    OnPropertyChanged();
                }
                catch (Exception) { }
            }
        }

        public Script Script  // present for JSON serialization
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
                    OnPropertyChanged();

                    if (_listening)
                        if (_rerunIncompleteScripts)
                            StartScriptRerunTaskAsync();
                        else
                            StopRerunTaskAsync();
                }
            }
        }

        [EntryIntegerUiProperty("Script Rerun Delay (MS):", true, 11)]
        public int ScriptRerunDelayMS
        {
            get { return _scriptRerunDelayMS; }
            set
            {
                if (value != _scriptRerunDelayMS)
                {
                    _scriptRerunDelayMS = value;
                    OnPropertyChanged();
                }
            }
        }

        [EntryIntegerUiProperty("Max. Script Age (Mins.):", true, 12)]
        public int MaxScriptAgeMinutes
        {
            get { return _maxScriptAgeMinutes; }
            set
            {
                if (value != _maxScriptAgeMinutes)
                {
                    _maxScriptAgeMinutes = value;
                    OnPropertyChanged();
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

        public ListeningScriptProbe()
        {
            _triggers = new ObservableCollection<Trigger>();
            _triggerHandler = new Dictionary<Trigger, EventHandler<Tuple<Datum, Datum>>>();
            _listening = false;
            _incompleteScripts = new Queue<Script>();
            _rerunIncompleteScripts = false;
            _scriptRerunDelayMS = 60000;
            _stopScriptRerunTask = true;
            _maxScriptAgeMinutes = 10;

            _triggers.CollectionChanged += (o, e) =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Add)
                        foreach (Trigger addedTrigger in e.NewItems)
                        {
                            // ignore duplicate triggers -- the user should delete and re-add them instead.
                            if (_triggerHandler.ContainsKey(addedTrigger))
                                return;

                            EventHandler<Tuple<Datum, Datum>> handler = async (oo, prevCurrDatum) =>
                                {
                                    // must be listening and must have a current datum
                                    lock (this)
                                        if (!_listening || prevCurrDatum.Item2 == null)
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
                                        await RunScriptAsync(_script.Copy(), prevDatum, currDatum);  // run a copy of the pristine script, since it will be filled in when run.
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

        protected override void StartListening()
        {
            lock (this)
            {
                if (_script == null)
                    throw new Exception("Script has not been set on " + GetType().FullName);

                if (_listening)
                    return;
                else
                    _listening = true;

                if (_rerunIncompleteScripts)
                    StartScriptRerunTaskAsync();
            }
        }

        private async void StartScriptRerunTaskAsync()
        {
            await StopRerunTaskAsync();

            SensusServiceHelper.Get().Logger.Log("Starting script rerun task.", LoggingLevel.Normal);

            _scriptRerunTask = Task.Run(async () =>
                {
                    _stopScriptRerunTask = false;

                    while (!_stopScriptRerunTask)
                    {
                        int msToSleep = _scriptRerunDelayMS;
                        while (!_stopScriptRerunTask && msToSleep > 0)
                        {
                            Thread.Sleep(1000);
                            msToSleep -= 1000;
                        }

                        if (!_stopScriptRerunTask)
                        {
                            Script scriptToRerun = null;
                            lock (_incompleteScripts)
                                while (scriptToRerun == null && _incompleteScripts.Count > 0)
                                {
                                    scriptToRerun = _incompleteScripts.Dequeue();
                                    TimeSpan scriptAge = DateTimeOffset.UtcNow - scriptToRerun.FirstRunTimeStamp;
                                    if (scriptAge.TotalMinutes > _maxScriptAgeMinutes)
                                    {
                                        SensusServiceHelper.Get().Logger.Log("Script \"" + scriptToRerun.Name + "\" has aged out.", LoggingLevel.Normal);
                                        scriptToRerun = null;
                                    }
                                }

                            if (scriptToRerun != null)
                                await RunScriptAsync(scriptToRerun, null, null);
                        }
                    }

                    SensusServiceHelper.Get().Logger.Log("Script rerun task has exited its while-loop.", LoggingLevel.Normal);
                });
        }

        private Task RunScriptAsync(Script script, Datum prevDatum, Datum currDatum)
        {
            return Task.Run(async () =>
                {
                    foreach (ScriptDatum scriptDatum in await script.RunAsync(prevDatum, currDatum))
                        if (scriptDatum != null)
                        {
                            scriptDatum.ProbeType = GetType().FullName;
                            StoreDatum(scriptDatum);
                        }

                    if (_rerunIncompleteScripts && !script.Complete)
                        lock (_incompleteScripts)
                            _incompleteScripts.Enqueue(script);
                });
        }

        private Task StopRerunTaskAsync()
        {
            return Task.Run(() =>
                {
                    if (_scriptRerunTask != null)
                    {
                        SensusServiceHelper.Get().Logger.Log("Stopping incomplete script task.", LoggingLevel.Normal);

                        _stopScriptRerunTask = true;
                        _scriptRerunTask.Wait();
                    }
                });
        }

        protected override void StopListening()
        {
            lock (this)
                if (_listening)
                {
                    _listening = false;
                    StopRerunTaskAsync().Wait();
                }
                else
                    return;
        }
    }
}
