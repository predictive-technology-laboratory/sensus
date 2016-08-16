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
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;
using SensusService.Probes.User.Scripts.ProbeTriggerProperties;

namespace SensusService.Probes.User.MicrosoftBand
{
    public class MicrosoftBandGyroscopeDatum : Datum
    {
        private double _angularX;
        private double _angularY;
        private double _angularZ;

        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty]
        public double AngularX
        {
            get
            {
                return _angularX;
            }

            set
            {
                _angularX = value;
            }
        }

        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty]
        public double AngularY
        {
            get
            {
                return _angularY;
            }

            set
            {
                _angularY = value;
            }
        }

        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty]
        public double AngularZ
        {
            get
            {
                return _angularZ;
            }

            set
            {
                _angularZ = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Angular X:  " + Math.Round(_angularX, 2) + ", Angular Y:  " + Math.Round(_angularY, 2) + ", Angular Z:  " + Math.Round(_angularZ, 2);
            }
        }

        /// <summary>
        /// For JSON.net deserialization.
        /// </summary>
        private MicrosoftBandGyroscopeDatum()
        {
        }

        public MicrosoftBandGyroscopeDatum(DateTimeOffset timestamp, double angularX, double angularY, double angularZ)
            : base(timestamp)
        {
            _angularX = angularX;
            _angularY = angularY;
            _angularZ = angularZ;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Angular X:  " + _angularX + Environment.NewLine +
                   "Angular Y:  " + _angularY + Environment.NewLine +
                   "Angular Z:  " + _angularZ;
        }
    }
}