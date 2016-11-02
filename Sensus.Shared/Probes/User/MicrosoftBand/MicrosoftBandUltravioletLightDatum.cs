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
using Microsoft.Band.Portable.Sensors;

namespace Sensus.Probes.User.MicrosoftBand
{
    public class MicrosoftBandUltravioletLightDatum : Datum
    {
        private UVIndexLevel _level;

        public UVIndexLevel Level
        {
            get
            {
                return _level;
            }

            set
            {
                _level = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Level:  " + _level;
            }
        }

        /// <summary>
        /// For JSON.net deserialization.
        /// </summary>
        private MicrosoftBandUltravioletLightDatum()
        {
        }

        public MicrosoftBandUltravioletLightDatum(DateTimeOffset timestamp, UVIndexLevel level)
            : base(timestamp)
        {
            _level = level;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Level:  " + _level;
        }
    }
}