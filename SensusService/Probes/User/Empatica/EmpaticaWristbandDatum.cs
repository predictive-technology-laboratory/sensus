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
using SensusService.Probes.User.Scripts.ProbeTriggerProperties;
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;

namespace SensusService.Probes.User.Empatica
{
    public class EmpaticaWristbandDatum : Datum
    {
        [NumberProbeTriggerProperty("X Acceleration")]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public float? AccelerationX { get; set; }

        [NumberProbeTriggerProperty("Y Acceleration")]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public float? AccelerationY { get; set; }

        [NumberProbeTriggerProperty("Z Acceleration")]
        [Anonymizable(null, new Type[] { typeof(DoubleRoundingOnesAnonymizer), typeof(DoubleRoundingTensAnonymizer) }, -1)]
        public float? AccelerationZ { get; set; }

        [NumberProbeTriggerProperty("Battery Level")]
        [Anonymizable(null, typeof(DoubleRoundingTensAnonymizer), false)]
        public double? BatteryLevel { get; set; }

        [BooleanProbeTriggerProperty("Blood Volume Pulse")]
        public float? BloodVolumePulse { get; set; }

        [NumberProbeTriggerProperty("Galvanic Skin Response")]
        public float? GalvanicSkinResponse { get; set; }

        [NumberProbeTriggerProperty("Inter-Beat Interval")]
        public float? InterBeatInterval { get; set; }

        [BooleanProbeTriggerProperty]
        public bool? Tag { get; set; }

        [NumberProbeTriggerProperty]
        public float? Temperature { get; set; }
        
        public override string DisplayDetail
        {
            get
            {
                return "X:" + AccelerationX + " Y:" + AccelerationY + " Z:" + AccelerationZ + " Bat:" + BatteryLevel + " BVP:" + BloodVolumePulse + " GSR:" + GalvanicSkinResponse + " IBI:" + InterBeatInterval;
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private EmpaticaWristbandDatum()
        {
        }

        public EmpaticaWristbandDatum(DateTimeOffset timestamp)
            : base(timestamp)
        {
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
            "X:  " + AccelerationX + Environment.NewLine +
            "Y:  " + AccelerationY + Environment.NewLine +
            "Z:  " + AccelerationZ + Environment.NewLine +
            "Battery:  " + BatteryLevel + Environment.NewLine +
            "BVP:  " + BloodVolumePulse + Environment.NewLine +
            "GSR:  " + GalvanicSkinResponse + Environment.NewLine +
            "IBI:  " + InterBeatInterval;
        }
    }
}