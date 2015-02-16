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

using SensusService.Probes;
using System;

namespace SensusService
{
    /// <summary>
    /// Represents a Datum that could be imprecisely measured.
    /// </summary>
    public abstract class ImpreciseDatum : Datum
    {
        private double _accuracy;

        /// <summary>
        /// Precision of the measurement associated with this Datum.
        /// </summary>
        public double Accuracy
        {
            get { return _accuracy; }
            set { _accuracy = value; }
        }

        protected ImpreciseDatum(Probe probe, DateTimeOffset timestamp, double accuracy)
            : base(probe, timestamp)
        {
            _accuracy = accuracy;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Imprecise:  true";
        }
    }
}
