using System;
using System.Collections.Generic;
using System.Reflection;

namespace SensusService.Probes.User
{
    public class ListeningScriptProbe : ListeningProbe, IScriptProbe
    {
        [Flags]
        public enum TriggerValueCondition
        {
            Change = 1,
            LessThan = 2,
            LessThanOrEqual = 4,
            Equal = 8,
            GreaterThanOrEqual = 16,
            GreaterThan = 32
        }

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

        public override IEnumerable<string> Triggers
        {
            get { return _triggerHandler.Keys; }
        }

        public ListeningScriptProbe()
        {
            _triggerHandler = new Dictionary<string, EventHandler<Tuple<Datum, Datum>>>();
            _listening = false;
        }

        public void AddTrigger(Probe probe, Type datumType, string datumProperty, TriggerValueCondition condition, object conditionValue)
        {
            RemoveTrigger(probe, datumType, datumProperty, condition, conditionValue);

            EventHandler<Tuple<Datum, Datum>> handler = (o, prevCurrDatum) =>
                {
                    lock (this)
                        if (!_listening || prevCurrDatum.Item2 == null)
                            return;

                    Datum prevDatum = prevCurrDatum.Item1;
                    Datum currDatum = prevCurrDatum.Item2;

                    PropertyInfo property = datumType.GetProperty(datumProperty);

                    object datumValueToCompare = property.GetValue(currDatum);

                    if (condition.HasFlag(TriggerValueCondition.Change))
                    {
                        if (prevDatum == null)
                            return;

                        try
                        {
                            datumValueToCompare = Convert.ToDouble(datumValueToCompare) - Convert.ToDouble(property.GetValue(prevDatum));
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

                    if (condition.HasFlag(TriggerValueCondition.Equal) && compareTo == 0 ||
                        condition.HasFlag(TriggerValueCondition.GreaterThan) && compareTo > 0 ||
                        condition.HasFlag(TriggerValueCondition.GreaterThanOrEqual) && compareTo >= 0 ||
                        condition.HasFlag(TriggerValueCondition.LessThan) && compareTo < 0 ||
                        condition.HasFlag(TriggerValueCondition.LessThanOrEqual) && compareTo <= 0)
                        _script.Run(prevDatum, currDatum);
                };

            _triggerHandler.Add(GetTriggerKey(datumType, datumProperty, condition, conditionValue), handler);
            probe.MostRecentDatumChanged += handler;
        }

        public void RemoveTrigger(Probe probe, Type datumType, string datumProperty, TriggerValueCondition condition, object conditionValue)
        {
            RemoveTrigger(probe, GetTriggerKey(datumType, datumProperty, condition, conditionValue));
        }

        public void RemoveTrigger(Probe probe, string triggerKey)
        {
            if (_triggerHandler.ContainsKey(triggerKey))
            {
                _triggerHandler.Remove(triggerKey);
                probe.MostRecentDatumChanged -= _triggerHandler[triggerKey];
            }
        }

        private string GetTriggerKey(Type datumType, string datumProperty, TriggerValueCondition condition, object conditionValue)
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
