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

using System;
using Newtonsoft.Json;

namespace SensusUI.Inputs
{
    public class InputDisplayCondition
    {
        private Input _input;
        private InputValueCondition _condition;
        private string _value;
        private bool _conjunction;

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

        public string Value
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

        public bool Conjunction
        {
            get
            {
                return _conjunction;
            }
            set
            {
                _conjunction = value;
            }
        }

        [JsonIgnore]
        public bool Satisfied
        {
            get
            {
                return _condition == InputValueCondition.InputComplete && _input.Complete ||
                _input.Value != null && _condition == InputValueCondition.ValueEqualTo && _input.Value.ToString() == _value ||
                _input.Value != null && _condition == InputValueCondition.ValueNotEqualTo && _input.Value.ToString() != _value;
            }
        }

        public InputDisplayCondition(Input input, InputValueCondition condition, string value, bool conjunction)
        {
            _input = input;
            _condition = condition;
            _value = value;
            _conjunction = conjunction;
        }

        public override string ToString()
        {
            return _input.Name + " " + _condition + (_value == null ? "" : " " + _value.ToString()) + " " + (_conjunction ? "(conjunctive)" : "(disjunctive)");
        }
    }
}