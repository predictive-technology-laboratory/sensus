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
//
using System;

namespace SensusService.Anonymization.Anonymizers
{
    public class DoubleRoundingAnonymizer : Anonymizer
    {
        private int _places;

        public override string DisplayText
        {
            get
            {
                "Round:  " + _places + " places";
            }
        }

        public DoubleRoundingAnonymizer(int places)
        {
            _places = places;
        }

        public override object Apply(object value, Protocol protocol)
        {
            double d = (double)value;

            if (_places > 0)
                return Math.Round(d, _places);
            else if (_places < 0)
                return (d / Math.Pow(10, _places)) * _places;
            else
                return d;
        }
    }
}

