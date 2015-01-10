using System;
using System.Collections.Generic;
using System.Reflection;

namespace SensusService.Probes.User
{
    public class ListeningScriptProbe : ListeningProbe, IScriptProbe
    {  
        private Script _script;
        private Dictionary<string, EventHandler<Tuple<Datum, Datum>>> _triggerHandler;
        private bool _listening;

        public string ScriptPath
        {
            get { return _script.Path; }
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
            get { return "User"; }
        }

        public IEnumerable<string> Triggers
        {
            get { return _triggerHandler.Keys; }
        }

        public sealed override Type DatumType
        {
            get { return typeof(ScriptDatum); }
        }

        public ListeningScriptProbe()
        {
            _triggerHandler = new Dictionary<string, EventHandler<Tuple<Datum, Datum>>>();
            _listening = false;
        }

        public void AddTrigger(Probe probe, PropertyInfo datumProperty, TriggerValueCondition condition, object conditionValue, bool change)
        {
            RemoveTrigger(probe, datumProperty, condition, conditionValue);

            EventHandler<Tuple<Datum, Datum>> handler = (o, prevCurrDatum) =>
                {
                    lock (this)
                        if (!_listening || prevCurrDatum.Item2 == null)
                            return;

                    Datum prevDatum = prevCurrDatum.Item1;
                    Datum currDatum = prevCurrDatum.Item2;

                    object datumValueToCompare = datumProperty.GetValue(currDatum);

                    if (change)
                    {
                        if (prevDatum == null)
                            return;

                        try
                        {
                            datumValueToCompare = Convert.ToDouble(datumValueToCompare) - Convert.ToDouble(datumProperty.GetValue(prevDatum));
                            conditionValue = Convert.ToDouble(conditionValue);
                        }
                        catch (Exception ex)
                        {
                            SensusServiceHelper.Get().Logger.Log("Failed to convert datum values to doubles:  " + ex.Message, LoggingLevel.Normal);
                            return;
                        }
                    }

                    int compareTo;
                    try { compareTo = ((IComparable)datumValueToCompare).CompareTo(conditionValue); }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Failed to compare datum values:  " + ex.Message, LoggingLevel.Normal);
                        return;
                    }

                    if (condition == TriggerValueCondition.Equal && compareTo == 0 ||
                        condition == TriggerValueCondition.GreaterThan && compareTo > 0 ||
                        condition == TriggerValueCondition.GreaterThanOrEqual && compareTo >= 0 ||
                        condition == TriggerValueCondition.LessThan && compareTo < 0 ||
                        condition == TriggerValueCondition.LessThanOrEqual && compareTo <= 0)
                        _script.Run(prevDatum, currDatum);
                };

            _triggerHandler.Add(GetTriggerKey(probe.DatumType, datumProperty, condition, conditionValue), handler);
            probe.MostRecentDatumChanged += handler;
        }

        public void RemoveTrigger(Probe probe, PropertyInfo datumProperty, TriggerValueCondition condition, object conditionValue)
        {
            RemoveTrigger(probe, GetTriggerKey(probe.DatumType, datumProperty, condition, conditionValue));
        }

        public void RemoveTrigger(Probe probe, string triggerKey)
        {
            if (_triggerHandler.ContainsKey(triggerKey))
            {
                _triggerHandler.Remove(triggerKey);
                probe.MostRecentDatumChanged -= _triggerHandler[triggerKey];
            }
        }

        private string GetTriggerKey(Type datumType, PropertyInfo datumProperty, TriggerValueCondition condition, object conditionValue)
        {
            return datumType.FullName + "-" + datumProperty + "-" + condition + "-" + conditionValue;
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
