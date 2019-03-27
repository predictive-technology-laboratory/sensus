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
using Sensus.Anonymization;
using Sensus.Anonymization.Anonymizers;
using Sensus.Probes.User.Scripts.ProbeTriggerProperties;

namespace Sensus.Probes.Device
{
    public class ProcessorUtilizationDatum : Datum
    {
        private double _cpuPercent;

        [DoubleProbeTriggerProperty]
        [Anonymizable(null, typeof(DoubleRoundingTensAnonymizer), false)]
        public double CpuPercent
        {
            get { return _cpuPercent; }
            set { _cpuPercent = value; }
        }

        public override string DisplayDetail
        {
            get { return "Cpu Utilization:  " + _cpuPercent +"%"; }
        }

        /// <summary>
        /// Gets the string placeholder value, which is the CPU Pervent.
        /// </summary>
        /// <value>The string placeholder value.</value>
        public override object StringPlaceholderValue
        {
            get
            {
                return _cpuPercent+ "%";
            }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private ProcessorUtilizationDatum() { }

        public ProcessorUtilizationDatum(DateTimeOffset timestamp, double cpuPercent)
            : base(timestamp)
        {
            _cpuPercent = cpuPercent;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   DisplayDetail;
        }
    }
}
