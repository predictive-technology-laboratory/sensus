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
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;
using System;
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;

namespace Sensus.Probes.Location
{
    public class AltitudeDatum : ImpreciseDatum
    {
        private double _altitude;

        [DoubleProbeTriggerProperty]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingTensAnonymizer), typeof(DoubleRoundingHundredsAnonymizer) }, -1)]
        public double Altitude
        {
            get { return _altitude; }
            set { _altitude = value; }
        }

        public override string DisplayDetail
        {
            get { return Math.Round(_altitude, 0) + " feet"; }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the altitude.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return Math.Round(_altitude, 0);
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private AltitudeDatum() { }

        public AltitudeDatum(DateTimeOffset timestamp, double accuracy, double altitude)
            : base(timestamp, accuracy)
        {
            _altitude = altitude;
        }

        public AltitudeDatum(DateTimeOffset timestamp, double hPa)
            : base(timestamp, -1) //-1 indicates normal accuracy
        {
            //hPa is a pressure reading not an altitude reading, the formula below converts it into an altitude
            double stdPressure = 1013.25;
            double altitude = (1 - Math.Pow((hPa / stdPressure), 0.190284)) * 145366.45;

            _altitude = altitude;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Altitude:  " + _altitude + " feet";
        }
    }
}
