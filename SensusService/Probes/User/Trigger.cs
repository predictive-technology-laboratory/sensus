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
using System.Reflection;

namespace SensusService.Probes.User
{
    /// <summary>
    /// Represents a condition under which a scripted probe is run.
    /// </summary>
    public class Trigger
    {
        private Probe _probe;
        private string _datumPropertyName;
        private TriggerValueCondition _condition;
        private object _conditionValue;
        private Type _conditionValueEnumType;
        private bool _change;
        private bool _fireRepeatedly;
        private bool _conditionSatisfiedLastTime;

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
            set
            {
                _conditionValue = value;

                // convert to enumerated type if we have the string type name
                if (_conditionValueEnumType != null)
                    _conditionValue = Enum.ToObject(_conditionValueEnumType, _conditionValue);
            }
        }

        /// <summary>
        /// This is a workaround for an odd behavior of JSON.NET, which serializes enumerations as integers. We happen to be deserializing them as objects
        /// into ConditionValue, which means they are stored as integers after deserialization. Integers are not comparable with the enumerated values 
        /// that come off the probes, so we need to jump through some hoops during deserization (i.e., below and above). Below, gettings and setting the
        /// value works off of the enumerated type that should be used for the ConditionValue above. When either the below or above are set, they check
        /// for the existence of the other and convert the number returned by JSON.NET to its appropriate enumerated type.
        /// </summary>
        public string ConditionValueEnumType
        {
            get { return _conditionValue is Enum ? _conditionValue.GetType().FullName : null; }
            set
            {
                if(value != null)
                {
                    _conditionValueEnumType = Assembly.GetExecutingAssembly().GetType(value);

                    // convert to enumerated type if we have the integer value
                    if (_conditionValue != null)
                        _conditionValue = Enum.ToObject(_conditionValueEnumType, _conditionValue);
                }
            }
        }

        public bool Change
        {
            get { return _change; }
            set { _change = value; }
        }

        public bool FireRepeatedly
        {
            get { return _fireRepeatedly; }
            set { _fireRepeatedly = value; }
        }

        private Trigger() { }  // for JSON deserialization

        public Trigger(Probe probe, string datumPropertyName, TriggerValueCondition condition, object conditionValue, bool change, bool fireRepeatedly)
        {
            _probe = probe;
            _datumPropertyName = datumPropertyName;
            _condition = condition;
            _conditionValue = conditionValue;
            _change = change;
            _fireRepeatedly = fireRepeatedly;
            _conditionSatisfiedLastTime = false;
        }

        public bool FireFor(object value)
        {
            lock (this)
            {
                bool conditionSatisfied = false;

                int compareTo;
                try { compareTo = ((IComparable)value).CompareTo(_conditionValue); }
                catch (Exception ex)
                {
                    SensusServiceHelper.Get().Logger.Log("Trigger failed to compare values:  " + ex.Message, LoggingLevel.Normal);
                    return false;
                }

                conditionSatisfied = _condition == TriggerValueCondition.Equal && compareTo == 0 ||
                                     _condition == TriggerValueCondition.GreaterThan && compareTo > 0 ||
                                     _condition == TriggerValueCondition.GreaterThanOrEqual && compareTo >= 0 ||
                                     _condition == TriggerValueCondition.LessThan && compareTo < 0 ||
                                     _condition == TriggerValueCondition.LessThanOrEqual && compareTo <= 0;

                bool fire = false;

                if (conditionSatisfied)
                    if (_fireRepeatedly || !_conditionSatisfiedLastTime)
                        fire = true;

                _conditionSatisfiedLastTime = conditionSatisfied;

                return fire;
            }
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
