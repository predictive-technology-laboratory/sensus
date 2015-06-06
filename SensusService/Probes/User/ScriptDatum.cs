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

namespace SensusService.Probes.User
{
    public class ScriptDatum : Datum
    {
        private string _response;
        private string _triggerDatumId;

        [Anonymizable(null, typeof(StringHashAnonymizer), true)]
        public string Response
        {
            get { return _response; }
            set { _response = value; }
        }

        [Anonymizable("Triggering Datum ID:", typeof(StringHashAnonymizer), false)]
        public string TriggerDatumId
        {
            get { return _triggerDatumId; }
            set { _triggerDatumId = value; }
        }

        public override string DisplayDetail
        {
            get { return _response; }
        }

        /// <summary>
        /// For JSON deserialization.
        /// </summary>
        private ScriptDatum() { }

        public ScriptDatum(DateTimeOffset timestamp, string response, string triggerDatumId)
            : base(timestamp)
        {
            _response = response == null ? "" : response;
            _triggerDatumId = triggerDatumId == null ? "" : triggerDatumId;
        }

        public override string ToString()
        {
            return base.ToString() + Environment.NewLine +
                   "Response:  " + _response;
        }
    }
}
