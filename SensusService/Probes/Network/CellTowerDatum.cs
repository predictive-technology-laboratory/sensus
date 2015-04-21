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
using SensusService.Anonymization;
using SensusService.Anonymization.Anonymizers;

namespace SensusService.Probes.Network
{
    public class CellTowerDatum : Datum
    {
        private string _cellTower;

        [TextProbeTriggerProperty]
        [Anonymizable("Cellular Tower", typeof(StringMD5Anonymizer), false)]
        public string CellTower
        {
            get { return _cellTower; }
            set { _cellTower = value; }
        }

        [JsonIgnore]
        public override string DisplayDetail
        {
            get { return _cellTower; }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private CellTowerDatum() { }

        public CellTowerDatum(DateTimeOffset timestamp, string cellTower)
            : base(timestamp)
        {
            _cellTower = cellTower == null ? "" : cellTower;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Cell Tower:  " + _cellTower;
        }
    }
}
