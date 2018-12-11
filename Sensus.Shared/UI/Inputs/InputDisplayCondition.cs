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

using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace Sensus.UI.Inputs
{
    public class InputDisplayCondition
    {
        private Input _input;
        private InputValueCondition _condition;
        private object _value;
        private bool _conjunctive;

        public Input Input
        {
            get
            {
                return _input;
            }
            set
            {
                _input = value;
            }
        }

        public InputValueCondition Condition
        {
            get
            {
                return _condition;
            }
            set
            {
                _condition = value;
            }
        }

        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        public bool Conjunctive
        {
            get
            {
                return _conjunctive;
            }
            set
            {
                _conjunctive = value;
            }
        }

        [JsonIgnore]
        public bool Satisfied
        {
            get
            {
                return _condition == InputValueCondition.IsComplete && _input.Complete ||
                _condition == InputValueCondition.Equals && _input.ValueMatches(_value, _conjunctive) ||
                _condition == InputValueCondition.DoesNotEqual && !_input.ValueMatches(_value, _conjunctive);
            }
        }

        public InputDisplayCondition(Input input, InputValueCondition condition, object value, bool conjunctive)
        {
            _input = input;
            _condition = condition;
            _value = value;
            _conjunctive = conjunctive;
        }

        public override string ToString()
        {
            string valueDescription = "";

            if (_value != null)
            {
                if (_value is List<object>)
                {
                    valueDescription = string.Concat((_value as List<object>).Select(o => "," + o).ToArray()).Trim(',');
                }
                else
                {
                    valueDescription = _value.ToString();
                }

                valueDescription = " " + valueDescription;
            }

            return _input.Name + " " + _condition + valueDescription + " " + (_conjunctive ? "(Conjunctive)" : "(Disjunctive)");
        }
    }
}
