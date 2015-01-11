
using Newtonsoft.Json;
using System;
using System.Reflection;
namespace SensusService.Probes.User
{
    public class Trigger
    {
        private Probe _probe;
        private string _datumPropertyName;
        private TriggerValueCondition _condition;
        private object _conditionValue;
        private bool _change;

        public Probe Probe
        {
            get { return _probe; }
            set { _probe = value; }
        }

        public string DatumPropertyName
        {
            get { return _datumPropertyName; }
            set { _datumPropertyName = value; }
        }

        [JsonIgnore]
        public PropertyInfo DatumProperty
        {
            get { return _probe.DatumType.GetProperty(_datumPropertyName); }
        }

        public TriggerValueCondition Condition
        {
            get { return _condition; }
            set { _condition = value; }
        }


        public object ConditionValue
        {
            get { return _conditionValue; }
            set { _conditionValue = value; }
        }

        public bool Change
        {
            get { return _change; }
            set { _change = value; }
        }

        public Trigger(Probe probe, string datumPropertyName, TriggerValueCondition condition, object conditionValue, bool change)
        {
            _probe = probe;
            _datumPropertyName = datumPropertyName;
            _condition = condition;
            _conditionValue = conditionValue;
            _change = change;
        }

        public bool FiresFor(object value)
        {
            int compareTo;
            try { compareTo = ((IComparable)value).CompareTo(_conditionValue); }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log("Trigger failed to compare values:  " + ex.Message, LoggingLevel.Normal);
                return false;
            }

            return _condition == TriggerValueCondition.Equal && compareTo == 0 ||
                   _condition == TriggerValueCondition.GreaterThan && compareTo > 0 ||
                   _condition == TriggerValueCondition.GreaterThanOrEqual && compareTo >= 0 ||
                   _condition == TriggerValueCondition.LessThan && compareTo < 0 ||
                   _condition == TriggerValueCondition.LessThanOrEqual && compareTo <= 0;
        }

        public override string ToString()
        {
            return _probe.DisplayName + " (" + _datumPropertyName + " " + _condition + " " + _conditionValue + ")";
        }

        public override bool Equals(object obj)
        {
            Trigger trigger = obj as Trigger;

            return trigger != null &&
                   _probe == trigger.Probe &&
                   _datumPropertyName == trigger.DatumPropertyName &&
                   _condition == trigger.Condition &&
                   _conditionValue == trigger.ConditionValue &&
                   _change == trigger.Change;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
