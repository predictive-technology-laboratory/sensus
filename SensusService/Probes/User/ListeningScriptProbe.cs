using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SensusService.Probes.User
{
    public class ListeningScriptProbe : ListeningProbe
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
        private Dictionary<string, Action<Tuple<Datum, Datum>>> _triggers;

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

        public ListeningScriptProbe()
        {
            _triggers = new Dictionary<string, Action<Tuple<Datum, Datum>>>();
        }

        public void AddTrigger(Probe probe, Type datumType, string datumProperty, TriggerValueCondition condition, object conditionValue)
        {
            probe.MostRecentDatumChanged += (o, prevCurrDatum) =>
                {
                    Datum prevDatum = prevCurrDatum.Item1;
                    Datum currDatum = prevCurrDatum.Item2;

                    PropertyInfo property = datumType.GetProperty(datumProperty);

                    object datumValueToCompare = property.GetValue(currDatum);

                    if (condition.HasFlag(TriggerValueCondition.Change))
                    {
                        datumValueToCompare = Convert.ToDouble(datumValueToCompare) - Convert.ToDouble(property.GetValue(prevDatum));
                        conditionValue = Convert.ToDouble(conditionValue);
                    }

                    int compareTo = ((IComparable)datumValueToCompare).CompareTo(conditionValue);

                    if (condition.HasFlag(TriggerValueCondition.Equal) && compareTo == 0 ||
                        condition.HasFlag(TriggerValueCondition.GreaterThan) && compareTo > 0 ||
                        condition.HasFlag(TriggerValueCondition.GreaterThanOrEqual) && compareTo >= 0 ||
                        condition.HasFlag(TriggerValueCondition.LessThan) && compareTo < 0 ||
                        condition.HasFlag(TriggerValueCondition.LessThanOrEqual) && compareTo <= 0)
                        _script.Run(prevDatum, currDatum);
                };
        }

        public void RemoveTrigger(Probe probe, Type datumType, string datumProperty, TriggerValueCondition condition)
        {
        }

        protected override void StartListening()
        {
            throw new NotImplementedException();
        }

        protected override void StopListening()
        {
            throw new NotImplementedException();
        }
    }
}
