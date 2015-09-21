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
using System;
using System.Reflection;
using System.Text.RegularExpressions;

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
        private bool _fireCriteriaMetOnPreviousCall;
        private Regex _regularExpression;
        private bool _ignoreFirstDatum;
        private bool _firstDatum;
        private TimeSpan _startTime;
        private TimeSpan _endTime;

        private readonly object _locker = new object();

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
                if (value != null)
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

        public bool FireCriteriaMetOnPreviousCall
        {
            get
            {
                return _fireCriteriaMetOnPreviousCall;
            }
            set
            {
                _fireCriteriaMetOnPreviousCall = value;
            }
        }

        public string RegularExpressionText
        {
            get { return _regularExpression == null ? null : _regularExpression.ToString(); }
            set
            {
                if (value != null)
                    _regularExpression = new Regex(value);
            }
        }

        public bool IgnoreFirstDatum
        {
            get { return _ignoreFirstDatum; }
            set { _ignoreFirstDatum = value; }
        }

        public TimeSpan StartTime
        {
            get { return _startTime; }
            set { _startTime = value; }
        }

        public TimeSpan EndTime
        {
            get { return _endTime; }
            set { _endTime = value; }
        }

        private Trigger()
        {
            Reset();
        }

        public Trigger(Probe probe, PropertyInfo datumProperty, TriggerValueCondition condition, object conditionValue, bool change, bool fireRepeatedly, bool useRegularExpressions, bool ignoreFirstDatum, TimeSpan startTime, TimeSpan endTime)
            : this()
        {
            if (probe == null)
                throw new Exception("Trigger is missing Probe selection.");
            else if (datumProperty == null)
                throw new Exception("Trigger is missing Property selection.");
            else if (conditionValue == null)
                throw new Exception("Trigger is missing Value selection.");
            else if (endTime <= startTime)
                throw new Exception("Trigger Start Time must precede End Time.");
            
            _probe = probe;
            _datumPropertyName = datumProperty.Name;
            _condition = condition;
            _conditionValue = conditionValue;
            _change = change;
            _fireRepeatedly = fireRepeatedly;
            _ignoreFirstDatum = ignoreFirstDatum;
            _startTime = startTime;
            _endTime = endTime;

            if (useRegularExpressions)
                _regularExpression = new Regex(_conditionValue.ToString());
        }

        public void Reset()
        {
            _fireCriteriaMetOnPreviousCall = false;
            _firstDatum = true;
        }

        public bool FireFor(object value)
        {
            lock (_locker)
            {
                bool conditionSatisfied;

                if (_regularExpression == null)
                {
                    int compareTo;
                    try
                    {
                        compareTo = ((IComparable)value).CompareTo(_conditionValue);
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Trigger failed to compare values:  " + ex.Message, LoggingLevel.Normal, GetType());
                        return false;
                    }

                    conditionSatisfied = _condition == TriggerValueCondition.Equal && compareTo == 0 ||
                    _condition == TriggerValueCondition.GreaterThan && compareTo > 0 ||
                    _condition == TriggerValueCondition.GreaterThanOrEqual && compareTo >= 0 ||
                    _condition == TriggerValueCondition.LessThan && compareTo < 0 ||
                    _condition == TriggerValueCondition.LessThanOrEqual && compareTo <= 0;
                }
                else
                {
                    try
                    {
                        conditionSatisfied = _regularExpression.Match(value.ToString()).Success;
                    }
                    catch (Exception ex)
                    {
                        SensusServiceHelper.Get().Logger.Log("Trigger failed to run Regex.Match:  " + ex.Message, LoggingLevel.Normal, GetType());
                        return false;
                    }
                }

                // the processing of repeated fires depends on first-datum status as well as satisfaction of the condition
                bool fireCriteriaMet = (!_ignoreFirstDatum || !_firstDatum) &&
                                       conditionSatisfied;

                // whether or not to fire depends on the fire criteria, preferences about repeats, and time restrictions
                bool fire = fireCriteriaMet &&
                            (_fireRepeatedly || !_fireCriteriaMetOnPreviousCall) &&
                            (DateTime.Now.TimeOfDay >= _startTime && DateTime.Now.TimeOfDay <= _endTime);

                _fireCriteriaMetOnPreviousCall = fireCriteriaMet;
                _firstDatum = false;

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
