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

namespace SensusUI.Inputs
{
    public class InputCompletionRecord
    {
        private DateTimeOffset _timestamp;
        private object _value;

        public DateTimeOffset Timestamp
        {
            get
            {
                return _timestamp;
            }
            set
            {
                _timestamp = value;
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

        /// <summary>
        /// For JSON serialization.
        /// </summary>
        private InputCompletionRecord()
        {
        }

        public InputCompletionRecord(DateTimeOffset timestamp, object value)
            : this()
        {
            _timestamp = timestamp;
            _value = value;
        }
    }
}