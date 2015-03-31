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

namespace SensusService.Anonymization.Anonymizers
{
    public abstract class DoubleRoundingAnonymizer : Anonymizer
    {
        private int _places;

        public override string DisplayText
        {
            get
            {
                return "Round:  " + _places + " places";
            }
        }

        protected DoubleRoundingAnonymizer(int places)
        {
            _places = places;
        }

        public override object Apply(object value, Protocol protocol)
        {
            double doubleValue = (double)value;

            if (_places >= 0)
                return Math.Round(doubleValue, _places);
            else
            {
                // round number to nearest 10^(-_places). for example, -1 would round to tens place, -2 to hundreds, etc.
                doubleValue = doubleValue * Math.Pow(10d, (double)_places);  // shift target place into one position (remember, _places is negative)
                doubleValue = Math.Round(doubleValue, 0);                    // round off fractional part
                return doubleValue / Math.Pow(10d, (double)_places);         // shift target place back to its proper location (remember, _places is negative)
            }
        }
    }
}