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

namespace SensusService.Probes.User.Scripts.MicrosoftBand
{
    public class MicrosoftBandPedometerDatum : Datum
    {
        private long _totalSteps;

        public long TotalSteps
        {
            get
            {
                return _totalSteps;
            }

            set
            {
                _totalSteps = value;
            }
        }

        public override string DisplayDetail
        {
            get
            {
                return "Total Steps:  " + _totalSteps;
            }
        }

        /// <summary>
        /// For JSON.net deserialization.
        /// </summary>
        private MicrosoftBandPedometerDatum()
        {
        }

        public MicrosoftBandPedometerDatum(DateTimeOffset timestamp, long totalSteps)
            : base(timestamp)
        {
            _totalSteps = totalSteps;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Total Steps:  " + _totalSteps;
        }
    }
}