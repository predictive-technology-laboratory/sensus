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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace SensusService.Probes.User
{
    public class ListeningScriptProbe : ListeningProbe, IScriptProbe
    {  
        private ObservableCollection<Trigger> _triggers;
        private Dictionary<Trigger, EventHandler<Tuple<Datum, Datum>>> _triggerHandler;
        private bool _listening;
        private Script _script;

        public string ScriptPath
        {
            get { return _script == null ? null : _script.Path; }
            set
            {
                if (_script == null || _script.Path != value)
                {
                    _script = new Script(value);
                    OnPropertyChanged();

                    DisplayName = _script.Name;
                }
            }
        }

        protected override string DefaultDisplayName
        {
            get { return "User Interaction"; }
        }

        public ObservableCollection<Trigger> Triggers
        {
            get { return _triggers; }
            set
            {
                lock (this)
                {
                    if (_triggers != null)
                    {
                        foreach (Trigger trigger in _triggers)
                            RemoveTrigger(trigger);
                    }

                    foreach (Trigger trigger in value)
                        AddTrigger(trigger);
                }
            }
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
        }

        public void AddTrigger(Probe probe, PropertyInfo datumProperty, TriggerValueCondition condition, object conditionValue, bool change)
        {
            AddTrigger(new Trigger(probe, datumProperty.Name, condition, conditionValue, change));
        }

        public void AddTrigger(Trigger trigger)
        {
            RemoveTrigger(trigger);

            EventHandler<Tuple<Datum, Datum>> handler = (o, prevCurrDatum) =>
                {
                    lock (this)
                        if (!_listening || prevCurrDatum.Item2 == null)
                            return;

                    Datum prevDatum = prevCurrDatum.Item1;
                    Datum currDatum = prevCurrDatum.Item2;

                    object datumValueToCompare = trigger.DatumProperty.GetValue(currDatum);

                    if (trigger.Change)
                    {
                        if (prevDatum == null)
                            return;

                        try { datumValueToCompare = Convert.ToDouble(datumValueToCompare) - Convert.ToDouble(trigger.DatumProperty.GetValue(prevDatum)); }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to convert datum values to doubles:  " + ex.Message, LoggingLevel.Normal);
                            return;
                        }
                    }

                    if (trigger.FiresFor(datumValueToCompare))
                        _script.Run(prevDatum, currDatum);
                };

            trigger.Probe.MostRecentDatumChanged += handler;
            _triggers.Add(trigger);
            _triggerHandler.Add(trigger, handler);
        }

        public void RemoveTrigger(Trigger trigger)
        {
            lock (this)
                if (_triggerHandler.ContainsKey(trigger))
                {
                    trigger.Probe.MostRecentDatumChanged -= _triggerHandler[trigger];
                    _triggers.Remove(trigger);
                    _triggerHandler.Remove(trigger);
                }
        }

        protected override void StartListening()
        {
            lock (this)
                if (_listening)
                    return;
                else
                    _listening = true;
        }

        protected override void StopListening()
        {
            lock (this)
                if (_listening)
                    _listening = false;
                else
                    return;
        }
    }
}
