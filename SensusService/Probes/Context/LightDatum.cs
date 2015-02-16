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
using SensusService.Probes.User.ProbeTriggerProperties;
using System;

namespace SensusService.Probes.Context
{
    public class LightDatum : Datum
    {
        private double _brightness;

        [NumberProbeTriggerProperty]
        public double Brightness
        {
            get { return _brightness; }
            set { _brightness = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return "Brightness:  " + _brightness; }
        }

        public LightDatum(Probe probe, DateTimeOffset timestamp, double brightness)
            : base(probe, timestamp)
        {
            _brightness = brightness;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Brightness:  " + _brightness;
        }
    }
}
