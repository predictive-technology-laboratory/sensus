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
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;
using SensusService.Probes.User.Scripts.ProbeTriggerProperties;

namespace SensusService.Probes.User.Scripts.MicrosoftBand
{
    public class MicrosoftBandDistanceDatum : Datum
    {
        private double _totalDistance;
        private MotionType _motionType;

        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        [DoubleProbeTriggerProperty]
        public double TotalDistance
        {
            get
            {
                return _totalDistance;
            }

            set
            {
                _totalDistance = value;
            }
        }

        public MotionType MotionType
        {
            get
            {
                return _motionType;
            }

            set
            {
                _motionType = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Total Distance:  " + Math.Round(_totalDistance, 1) + ", Motion Type:  " + _motionType;
            }
        }

        /// <summary>
        /// For JSON.net deserialization.
        /// </summary>
        private MicrosoftBandDistanceDatum()
        {
        }

        public MicrosoftBandDistanceDatum(DateTimeOffset timestamp, double totalDistance, MotionType motionType)
            : base(timestamp)
        {
            _totalDistance = totalDistance;
            _motionType = motionType;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Total Distance:  " + _totalDistance + Environment.NewLine +
                   "Motion Type:  " + _motionType;
        }
    }
}