//Copyright 2014 The Rector & Visitors of the University of Virginia
//
//Permission is hereby granted, free of charge, to any person obtaining a copy 
//of this software and associated documentation files (the "Software"), to deal 
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
//copies of the Software, and to permit persons to whom the Software is 
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in 
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
//PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
//OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
//SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Sensus.Probes.User.Scripts
{
    /// <summary>
    /// Represents a condition under which a scripted probe is run.
    /// </summary>
    public class Trigger
    {
        private object _conditionValue;
        private Type _conditionValueEnumType;

        public Probe Probe { get; set; }

        public string DatumPropertyName { get; set; }

        [JsonIgnore]
        public PropertyInfo DatumProperty => Probe.DatumType.GetProperty(DatumPropertyName);

        public TriggerValueCondition Condition { get; set; }

        public object ConditionValue
        {
            get { return _conditionValue; }
            set
            {
                _conditionValue = value;

                // convert to enumerated type if we have the string type name
                if (_conditionValueEnumType != null)
                {
                    _conditionValue = Enum.ToObject(_conditionValueEnumType, _conditionValue);
                }
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
                    {
                        _conditionValue = Enum.ToObject(_conditionValueEnumType, _conditionValue);
                    }
                }
            }
        }

        public bool Change { get; set; }

        public bool FireRepeatedly { get; set; }

        public bool FireValueConditionMetOnPreviousCall { get; set; }

        public string RegularExpressionText { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        private Trigger()
        {
            Reset();
        }

        public Trigger(Probe probe, PropertyInfo datumProperty, TriggerValueCondition condition, object conditionValue, bool change, bool fireRepeatedly, bool useRegularExpressions, TimeSpan startTime, TimeSpan endTime) : this()
        {
            if (probe == null) throw new Exception("Trigger is missing Probe selection.");
            if (datumProperty == null) throw new Exception("Trigger is missing Property selection.");
            if (conditionValue == null) throw new Exception("Trigger is missing Value selection.");
            if (endTime <= startTime) throw new Exception("Trigger Start Time must precede End Time.");

            Probe = probe;
            DatumPropertyName = datumProperty.Name;
            Condition = condition;
            _conditionValue = conditionValue;
            Change = change;
            FireRepeatedly = fireRepeatedly;
            StartTime = startTime;
            EndTime = endTime;

            if (useRegularExpressions)
            {
                RegularExpressionText = _conditionValue.ToString();
            }
        }

        public void Reset()
        {
            FireValueConditionMetOnPreviousCall = false;
        }

        public bool FireFor(object value)
        {
            try
            {
                var fireValueConditionMet = FireValueConditionMet(value);
                var fireRepeatConditionMet = FireRepeatConditionMet();
                var fireWindowConditionMet = FireWindowConditionMet();

                FireValueConditionMetOnPreviousCall = fireValueConditionMet;

                return fireValueConditionMet && fireRepeatConditionMet && fireWindowConditionMet;
            }
            catch (Exception ex)
            {
                SensusServiceHelper.Get().Logger.Log(ex.Message, LoggingLevel.Normal, GetType());
                return false;
            }
        }

        public override string ToString()
        {
            // if a protocol is deserialized from a platform with probes that do not exist in the current platform
            // and if those probes are used in triggers, then we'll get null probe references in the triggers.
            // accommodate this below with the null conditional check on Probe.
            return $"{Probe?.DisplayName} ({DatumPropertyName} {Condition} {_conditionValue})";
        }

        public override bool Equals(object obj)
        {
            var trigger = obj as Trigger;

            return trigger != null && Probe == trigger.Probe && DatumPropertyName == trigger.DatumPropertyName && Condition == trigger.Condition && ConditionValue == trigger.ConditionValue && Change == trigger.Change;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        #region Private Methods
        private bool FireValueConditionMet(object value)
        {
            if (RegularExpressionText == null)
            {
                try
                {
                    var compareTo = ((IComparable)value).CompareTo(_conditionValue);

                    if (Condition == TriggerValueCondition.EqualTo) return compareTo == 0;
                    if (Condition == TriggerValueCondition.GreaterThan) return compareTo > 0;
                    if (Condition == TriggerValueCondition.GreaterThanOrEqualTo) return compareTo >= 0;
                    if (Condition == TriggerValueCondition.LessThan) return compareTo < 0;
                    if (Condition == TriggerValueCondition.LessThanOrEqualTo) return compareTo <= 0;
                    if (Condition == TriggerValueCondition.NotEqualTo) return compareTo != 0;

                    throw new Exception($"Trigger failed recognize Condition:  {Condition}");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Trigger failed to compare values:  {ex.Message}", ex);
                }
            }
            else
            {
                try
                {
                    return Regex.IsMatch(value.ToString(), RegularExpressionText);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Trigger failed to run Regex.Match:  {ex.Message}", ex);
                }
            }
        }

        private bool FireRepeatConditionMet()
        {
            return FireRepeatedly || !FireValueConditionMetOnPreviousCall;
        }

        private bool FireWindowConditionMet()
        {
            return StartTime <= DateTime.Now.TimeOfDay && DateTime.Now.TimeOfDay <= EndTime;
        }
        #endregion
    }
}
